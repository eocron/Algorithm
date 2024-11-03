namespace Eocron.Algorithms.HashCode;

public interface IHashProvider
{
    HashBytes GetOrCreateHash();
    
    HashBytes GetOrCreateHash(byte[] password);
    
    HashBytes GetOrCreateHash(byte[] password, int offset, int length);
}