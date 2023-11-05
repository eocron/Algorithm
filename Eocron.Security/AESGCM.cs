using System;
using System.IO;
using System.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Eocron.Security
{
  public static class AESGCM
  {
    private static readonly SecureRandom Random = new SecureRandom();

    //Preconfigured Encryption Parameters
    public static readonly int NonceBitSize = 128;
    public static readonly int MacBitSize = 128;
    public static readonly int KeyBitSize = 256;

    //Preconfigured Password Key Derivation Parameters
    public static readonly int SaltBitSize = 128;
    public static readonly int Iterations = 10000;
    public static readonly int MinPasswordLength = 12;

    private static IAeadCipher CreateAedCipher(byte[] key, byte[] nonce, byte[] nonSecretPayload)
    {
      IAeadCipher cipher = new GcmBlockCipher(new AesLightEngine());
      var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce, nonSecretPayload);
      cipher.Init(true, parameters);
    }
    public static byte[] SimpleEncrypt(byte[] secretMessage, byte[] key, byte[] nonSecretPayload = null)
    {
      //User Error Checks
      if (key == null || key.Length != KeyBitSize / 8)
        throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "key");

      if (secretMessage == null || secretMessage.Length == 0)
        throw new ArgumentException("Secret Message Required!", "secretMessage");

      //Non-secret Payload Optional
      nonSecretPayload = nonSecretPayload ?? Array.Empty<byte>();

      //Using random nonce large enough not to repeat
      var nonce = new byte[NonceBitSize / 8];
      Random.NextBytes(nonce, 0, nonce.Length);
      
      var cipher = CreateAedCipher(key, nonce, nonSecretPayload);
      //Generate Cipher Text With Auth Tag
      var cipherText = new byte[cipher.GetOutputSize(secretMessage.Length)];
      var len = cipher.ProcessBytes(secretMessage, 0, secretMessage.Length, cipherText, 0);
      cipher.DoFinal(cipherText, len);

      //Assemble Message
      using var combinedStream = new MemoryStream();
      using (var binaryWriter = new BinaryWriter(combinedStream))
      {
        //Prepend Authenticated Payload
        binaryWriter.Write(nonSecretPayload);
        //Prepend Nonce
        binaryWriter.Write(nonce);
        //Write Cipher Text
        binaryWriter.Write(cipherText);
      }

      return combinedStream.ToArray();
    }

    public static byte[] SimpleDecrypt(byte[] encryptedMessage, byte[] key, int nonSecretPayloadLength = 0)
    {
      //User Error Checks
      if (key == null || key.Length != KeyBitSize / 8)
        throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "key");

      if (encryptedMessage == null || encryptedMessage.Length == 0)
        throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");

      using var cipherStream = new MemoryStream(encryptedMessage);
      using var cipherReader = new BinaryReader(cipherStream);
      //Grab Payload
      var nonSecretPayload = cipherReader.ReadBytes(nonSecretPayloadLength);

      //Grab Nonce
      var nonce = cipherReader.ReadBytes(NonceBitSize / 8);

      var cipher = new GcmBlockCipher(new AesLightEngine());
      var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce, nonSecretPayload);
      cipher.Init(false, parameters);

      //Decrypt Cipher Text
      var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonSecretPayloadLength - nonce.Length);
      var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

      try
      {
        var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
        cipher.DoFinal(plainText, len);

      }
      catch (InvalidCipherTextException)
      {
        //Return null if it doesn't authenticate
        return null;
      }

      return plainText;
    }

    public static byte[] SimpleEncryptWithPassword(byte[] secretMessage, string password,
      byte[] nonSecretPayload = null)
    {
      nonSecretPayload = nonSecretPayload ?? new byte[] { };

      //User Error Checks
      if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        throw new ArgumentException(
          String.Format("Must have a password of at least {0} characters!", MinPasswordLength), "password");

      if (secretMessage == null || secretMessage.Length == 0)
        throw new ArgumentException("Secret Message Required!", "secretMessage");

      var generator = new Pkcs5S2ParametersGenerator();

      //Use Random Salt to minimize pre-generated weak password attacks.
      var salt = new byte[SaltBitSize / 8];
      Random.NextBytes(salt);

      generator.Init(
        PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
        salt,
        Iterations);

      //Generate Key
      var key = (KeyParameter)generator.GenerateDerivedMacParameters(KeyBitSize);

      //Create Full Non Secret Payload
      var payload = new byte[salt.Length + nonSecretPayload.Length];
      Array.Copy(nonSecretPayload, payload, nonSecretPayload.Length);
      Array.Copy(salt, 0, payload, nonSecretPayload.Length, salt.Length);

      return SimpleEncrypt(secretMessage, key.GetKey(), payload);
    }

    public static byte[] SimpleDecryptWithPassword(byte[] encryptedMessage, string password,
      int nonSecretPayloadLength = 0)
    {
      //User Error Checks
      if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        throw new ArgumentException(
          String.Format("Must have a password of at least {0} characters!", MinPasswordLength), "password");

      if (encryptedMessage == null || encryptedMessage.Length == 0)
        throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");

      var generator = new Pkcs5S2ParametersGenerator();

      //Grab Salt from Payload
      var salt = new byte[SaltBitSize / 8];
      Array.Copy(encryptedMessage, nonSecretPayloadLength, salt, 0, salt.Length);

      generator.Init(
        PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
        salt,
        Iterations);

      //Generate Key
      var key = (KeyParameter)generator.GenerateDerivedMacParameters(KeyBitSize);

      return SimpleDecrypt(encryptedMessage, key.GetKey(), salt.Length + nonSecretPayloadLength);
    }
  }
}