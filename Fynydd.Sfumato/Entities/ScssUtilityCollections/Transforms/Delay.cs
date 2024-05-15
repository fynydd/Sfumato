namespace Fynydd.Sfumato.Entities.ScssUtilityCollections.Transforms;

public class Delay : ScssUtilityClassGroupBase 
{
    public override string SelectorPrefix => "delay";

    public SfumatoAppState? AppState { get; set; }

    public override async Task InitializeAsync(SfumatoAppState appState)
    {
        AppState = appState;
        SelectorIndex.Add(SelectorPrefix);

        await AddToIndexAsync(appState.DelayStaticUtilities);
    }

    public override string GetStyles(CssSelector cssSelector)
    {
        if (cssSelector.AppState is null)
            return string.Empty;

        #region Static Utilities
        
        if (ProcessStaticDictionaryOptions(cssSelector.AppState.DelayStaticUtilities, cssSelector, AppState, out Result))
            return Result;
        
        #endregion
        
        #region Arbitrary Values
        
        if (cssSelector is not { HasArbitraryValue: true, CoreSegment: "" })
            return string.Empty;
        
        if (ProcessArbitraryValues("time", cssSelector, "transition-delay: {value};", AppState, out Result))
            return Result;
      
        #endregion

        return string.Empty;
    }
}