using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PvpIsolatedPlaytestTests
{
    static Type Owner => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType("PvpIsolatedPlaytest")).First(t => t != null);
    static bool Call(string method, params object[] args) =>
        (bool)Owner.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);

    [TestCase("11CB9E", "com.Orbyteon.HOL.pvptest", true, true, true)]
    [TestCase("195DDC", "com.Orbyteon.HOL.pvptest", true, true, false)]
    [TestCase("23E1A", "com.Orbyteon.HOL.pvptest", true, true, false)]
    [TestCase("", "com.Orbyteon.HOL.pvptest", true, true, false)]
    [TestCase("11CB9E", "com.Orbyteon.HOL", true, true, false)]
    [TestCase("11CB9E", "com.Orbyteon.HOL.pvptest", false, true, false)]
    [TestCase("11CB9E", "com.Orbyteon.HOL.pvptest", true, false, false)]
    public void OnlyExactDevelopmentAndroidTargetIsAccepted(string title, string package,
        bool android, bool development, bool expected)
    {
        Assert.AreEqual(expected, Call("IsValidTarget", title, package, android, development));
    }

    [TestCase("https://11CB9E.playfabapi.com/Client/LoginWithCustomID", true)]
    [TestCase("https://11CB9E.playfabapi.com/Client/ExecuteCloudScript", true)]
    [TestCase("https://195DDC.playfabapi.com/Client/LoginWithCustomID", false)]
    [TestCase("https://23E1A.playfabapi.com/Client/LoginWithCustomID", false)]
    [TestCase("https://11CB9E.playfabapi.com/Server/LoginWithCustomID", false)]
    [TestCase("https://11CB9E.playfabapi.com.evil.example/Client/LoginWithCustomID", false)]
    [TestCase("http://11CB9E.playfabapi.com/Client/LoginWithCustomID", false)]
    [TestCase("https://user@11CB9E.playfabapi.com/Client/LoginWithCustomID", false)]
    [TestCase("https://11CB9E.playfabapi.com:444/Client/LoginWithCustomID", false)]
    public void RequestGuardRejectsProductionAndServerEndpoints(string url, bool expected)
    {
        Assert.AreEqual(expected, Call("AllowsRequest", "11CB9E", url,
            "com.Orbyteon.HOL.pvptest", true, true));
    }

    [Test]
    public void IsolatedLoginCannotCreateAccountsOrInvokeProductionProvisioner()
    {
        foreach (bool development in new[] { false, true })
        foreach (bool requested in new[] { false, true })
            Assert.IsFalse(Call("AllowsClientCreation", true, development, requested));
        foreach (bool creates in new[] { false, true })
        foreach (bool permitted in new[] { false, true })
        foreach (bool missing in new[] { false, true })
            Assert.IsFalse(Call("AllowsProductionProvisioning", true, creates, permitted, missing));
        Assert.IsTrue(Call("AllowsClientCreation", false, true, true));
        Assert.IsFalse(Call("AllowsClientCreation", false, false, true));
        Assert.IsTrue(Call("AllowsProductionProvisioning", false, false, true, true));
        Assert.IsFalse(Call("AllowsProductionProvisioning", false, false, false, true));
    }

    [Test]
    public void NormalEditorClientIsNotReconfigured()
    {
        Assert.IsFalse((bool)Owner.GetProperty("Enabled").GetValue(null));
        var clientType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("PlayFabPvpClient")).First(t => t != null);
        var go = new GameObject("IsolatedConfigTest");
        try
        {
            var client = go.AddComponent(clientType);
            clientType.GetField("titleId").SetValue(client, "normal-editor-title");
            Owner.GetMethod("Configure").Invoke(null, new object[] { client });
            Assert.AreEqual("normal-editor-title", clientType.GetField("titleId").GetValue(client));
            Assert.IsTrue((bool)clientType.GetField("allowClientAccountCreationInDebugBuilds").GetValue(client));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
