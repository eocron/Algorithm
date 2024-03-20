using System.Collections.Generic;
using System.Linq;

namespace Eocron.Validation.Tests
{
    public static class TestHelper
    {
        public static IEnumerable<ValidationResult> VRs(params string[] message)
        {
            return message.Select(x => new ValidationResult() { Message = x, Type = ValidationResultType.Error });
        }
    }
}