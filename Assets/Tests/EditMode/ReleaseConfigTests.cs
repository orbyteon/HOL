using System;
using System.Reflection;
using NUnit.Framework;

public class ReleaseConfigTests
{
    static MethodInfo Validator()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType("ReleaseConfig");
            if (type == null) continue;
            var method = type.GetMethod("ValidateValues", BindingFlags.Public | BindingFlags.Static);
            if (method != null) return method;
        }

        Assert.Fail("ReleaseConfig.ValidateValues not found");
        return null;
    }

    static bool Validate(string titleId, string url, long projectNumber, out string error)
    {
        object[] args = { titleId, url, projectNumber, null };
        bool ok = (bool)Validator().Invoke(null, args);
        error = args[3] as string ?? "";
        return ok;
    }

    [Test]
    public void ValidProductionConfigPasses()
    {
        string error;
        Assert.IsTrue(Validate("1A2B3", "https://hol-provision.azurewebsites.net/api/provision",
            123456789012, out error), error);
    }

    [TestCase("", "https://example.com/api/provision", 123L)]
    [TestCase("BAD-ID", "https://example.com/api/provision", 123L)]
    [TestCase("1A2B3", "http://example.com/api/provision", 123L)]
    [TestCase("1A2B3", "https://user:pass@example.com/api/provision", 123L)]
    [TestCase("1A2B3", "https://example.com/api/provision", 0L)]
    public void InvalidProductionConfigFails(string titleId, string url, long projectNumber)
    {
        string error;
        Assert.IsFalse(Validate(titleId, url, projectNumber, out error));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }
}
