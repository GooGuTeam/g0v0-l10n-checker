// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Parsing;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class ResourceNameResolverTests
{
    private const string rootNamespace = "osu.Game.Resources.Localisation";

    [TestCase("Common.resx", "osu.Game.Resources.Localisation.Common")]
    [TestCase("HUD/SongProgress.resx", "osu.Game.Resources.Localisation.HUD.SongProgress")]
    [TestCase("Web/ModelValidation/Store/Product.resx", "osu.Game.Resources.Localisation.Web.ModelValidation.Store.Product")]
    [TestCase("Catch/Action.resx", "osu.Game.Resources.Localisation.Catch.Action")]
    public void Matches_the_names_the_SDK_actually_embeds(string relativePath, string expected)
    {
        string scanRoot = Path.Combine("C:", "repo", "osu.Game.Resources", "Localisation");
        string file = Path.Combine(scanRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.That(ResourceNameResolver.Resolve(file, scanRoot, rootNamespace), Is.EqualTo(expected));
    }

    [Test]
    public void Uses_the_path_alone_when_no_root_namespace_is_given()
    {
        string scanRoot = Path.Combine("C:", "repo", "Custom", "Localisation");
        string file = Path.Combine(scanRoot, "Thing.resx");

        Assert.That(ResourceNameResolver.Resolve(file, scanRoot, string.Empty), Is.EqualTo("Thing"));
    }

    [Test]
    public void Reports_the_relative_directory_of_a_resource()
    {
        string scanRoot = Path.Combine("C:", "repo", "Localisation");
        string file = Path.Combine(scanRoot, "HUD", "SpectatorList.resx");

        Assert.That(ResourceNameResolver.RelativeDirectory(file, scanRoot), Is.EqualTo("HUD"));
        Assert.That(ResourceNameResolver.RelativeDirectory(Path.Combine(scanRoot, "Common.resx"), scanRoot), Is.Empty);
    }
}
