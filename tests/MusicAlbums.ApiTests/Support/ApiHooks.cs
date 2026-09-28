using MusicAlbums.TestSupport;
using Reqnroll;

namespace MusicAlbums.ApiTests.Support;

[Binding]
public sealed class ApiHooks(ScenarioContext scenarioContext)
{
    [BeforeScenario]
    public void StartApi()
    {
        var factory = new MusicAlbumsApiFactory();
        scenarioContext.Set(factory);
        scenarioContext.Set(factory.CreateClient());
    }

    [AfterScenario]
    public void StopApi()
    {
        scenarioContext.Get<MusicAlbumsApiFactory>().Dispose();
    }
}
