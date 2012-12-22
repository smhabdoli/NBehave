using NBehave.Narrator.Framework;

namespace NBehave.VS2012.Plugin.Editor.Domain
{
    public class ScenarioGherkinText : IGherkinText
    {
        private readonly Scenario scenario;

        public ScenarioGherkinText(Scenario scenario)
        {
            this.scenario = scenario;
        }

        public string AsString { get { return scenario.ToString(); } }
        public string Title { get { return scenario.Title; } }
        public int SourceLine { get { return scenario.SourceLine; } }
    }
}