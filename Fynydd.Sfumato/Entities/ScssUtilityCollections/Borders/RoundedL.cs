namespace Fynydd.Sfumato.Entities.ScssUtilityCollections.Borders;

public class RoundedL : ScssUtilityClassGroupBase 
{
    public override string SelectorPrefix { get; set; } = "rounded-l";
    
    public SfumatoAppState? AppState { get; set; }

    public override async Task InitializeAsync(SfumatoAppState appState)
    {
        AppState = appState;
        SelectorIndex.Add(SelectorPrefix);

        await AddToIndexAsync(appState.RoundedOptions);
    }
    
    public override string GetStyles(CssSelector cssSelector)
    {
        if (cssSelector.AppState is null)
            return string.Empty;
        
        #region Calculated Utilities
        
        if (ProcessDictionaryOptions(cssSelector.AppState.RoundedOptions, cssSelector,
            """
            border-top-left-radius: {value};
            border-bottom-left-radius: {value};
            """, AppState, out Result))
            return Result;
        
        #endregion
        
        #region Arbitrary Values
        
        if (cssSelector is not { HasArbitraryValue: true, CoreSegment: "" })
            return string.Empty;
        
        if (ProcessArbitraryValues("length,percentage", cssSelector,
                """
                border-top-left-radius: {value};
                border-bottom-left-radius: {value};
                """, AppState, out Result))
            return Result;
        
        #endregion

        return string.Empty;
    }
}