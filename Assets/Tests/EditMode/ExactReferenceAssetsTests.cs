using NUnit.Framework;
using UnityEngine;

public class ExactReferenceAssetsTests
{
    [Test]
    public void ApprovedHolLogoLoadsAsSprite()
    {
        var logo = Resources.Load<Sprite>("reference/hol_logo_exact");
        Assert.IsNotNull(logo,
            "The approved HOL logo must import as a Sprite at Resources/reference/hol_logo_exact.");
    }
}
