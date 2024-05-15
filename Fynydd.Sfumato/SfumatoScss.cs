namespace Fynydd.Sfumato;

public static class SfumatoScss
{
	#region Core Shared SCSS

	/// <summary>
	/// Get all Sfumato base SCSS include files (e.g. browser reset, static element styles) and return as a single string.
	/// </summary>
	/// <param name="appState"></param>
	/// <param name="diagnosticOutput"></param>
	/// <returns></returns>
	public static async Task<string> GetBaseScssAsync(SfumatoAppState appState, ConcurrentDictionary<string,string> diagnosticOutput)
	{
		var timer = new Stopwatch();

		timer.Start();
		
		var sb = appState.StringBuilderPool.Get();
		
        try
        {
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_core.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_browser-reset.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_media-queries.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_initialize.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);
            sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_forms.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);

            ProcessShortCodes(appState, sb);
            
		    diagnosticOutput.TryAdd("init2", $"{Strings.TriangleRight} Prepared SCSS base for output injection in {timer.FormatTimer()}{Environment.NewLine}");

		    return sb.ToString();
        }

        catch
        {
            return string.Empty;
        }
        
        finally
        {
            appState.StringBuilderPool.Return(sb);
        }
	}
	
	/// <summary>
	/// Get all Sfumato core SCSS include files (e.g. mixins) and return as a single string.
	/// Used as a prefix for transpile in-place project SCSS files.
	/// </summary>
	/// <param name="appState"></param>
	/// <param name="diagnosticOutput"></param>
	/// <returns></returns>
	public static async Task<string> GetSharedScssAsync(SfumatoAppState appState, ConcurrentDictionary<string,string> diagnosticOutput)
	{
		var timer = new Stopwatch();

		timer.Start();
		
		var sb = appState.StringBuilderPool.Get();
		
        try
        {
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_core.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);
		    sb.Append((await Storage.ReadAllTextWithRetriesAsync(Path.Combine(appState.ScssPath, "_media-queries.scss"), SfumatoAppState.FileAccessRetryMs)).Trim() + Environment.NewLine);

            ProcessShortCodes(appState, sb);

		    diagnosticOutput.TryAdd("init3", $"{Strings.TriangleRight} Prepared shared SCSS for output injection in {timer.FormatTimer()}{Environment.NewLine}");

            return sb.ToString();
        }

        catch
        {
            return string.Empty;
        }
        
        finally
        {
            appState.StringBuilderPool.Return(sb);
        }
	}

    /// <summary>
    /// Find/replace various system short codes in generated SCSS markup
    /// (e.g. media breakpoint values).
    /// </summary>
    /// <param name="appState"></param>
    /// <param name="sb"></param>
    public static void ProcessShortCodes(SfumatoAppState appState, StringBuilder sb)
    {
        var breakpoints = appState.StringBuilderPool.Get();
        
        try
        {
            sb.Replace("#{zero-font-size}", $"{appState.Settings.Theme.FontSizeUnit?.Zero ?? "16px"}");

            sb.Replace("#{sm-bp}", $"{appState.Settings.Theme.MediaBreakpoint?.Sm}");
            sb.Replace("#{md-bp}", $"{appState.Settings.Theme.MediaBreakpoint?.Md}");
            sb.Replace("#{lg-bp}", $"{appState.Settings.Theme.MediaBreakpoint?.Lg}");
            sb.Replace("#{xl-bp}", $"{appState.Settings.Theme.MediaBreakpoint?.Xl}");
            sb.Replace("#{xxl-bp}", $"{appState.Settings.Theme.MediaBreakpoint?.Xxl}");
            
            sb.Replace("$internal-dark-theme: \"\";", $"$internal-dark-theme: \"{(appState.Settings.DarkMode.Equals("media", StringComparison.OrdinalIgnoreCase) ? "media" : appState.Settings.UseAutoTheme ? "class+auto" : "class")}\";");

            sb.Replace("$mobi-breakpoint: \"\";", $"$mobi-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "mobi").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            sb.Replace("$tabp-breakpoint: \"\";", $"$tabp-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "tabp").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            sb.Replace("$tabl-breakpoint: \"\";", $"$tabl-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "tabl").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            sb.Replace("$desk-breakpoint: \"\";", $"$desk-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "desk").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            sb.Replace("$wide-breakpoint: \"\";", $"$wide-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "wide").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            sb.Replace("$vast-breakpoint: \"\";", $"$vast-breakpoint: \"{appState.MediaQueryPrefixes.First(p => p.Prefix == "vast").Statement.TrimStart("@media ").TrimEnd("{")?.Trim()}\";");
            
            if (appState.Settings.Theme.UseAdaptiveLayout == false)
            {
                sb.Replace("$adaptive-breakpoint-font-sizes;", string.Empty);

                try
                {
                    foreach (var prefix in appState.MediaQueryPrefixes.Where(p => p.PrefixType.Equals("breakpoint", StringComparison.OrdinalIgnoreCase) && p.Prefix.Length < 4).OrderBy(o => o.PrefixOrder))
                    {
                        var fontSize = appState.Settings.Theme.FontSizeUnit?.GetType()
                            .GetProperty(prefix.Prefix.ToSentenceCase())
                            ?.GetValue(appState.Settings.Theme.FontSizeUnit, null)
                            ?.ToString() ?? string.Empty;

                        if (string.IsNullOrEmpty(fontSize))
                            fontSize = "16px";
                        
                        if (prefix.Prefix == "xxl")
                        {
                            if (fontSize.EndsWith("vw", StringComparison.Ordinal))
                            {
                                fontSize = $"calc(#{{$xxl-breakpoint}} * (#{{sf-strip-unit({appState.Settings.Theme.FontSizeUnit?.Xxl})}} / 100))";
                            }
            
                            else
                            {
                                fontSize = $"{appState.Settings.Theme.FontSizeUnit?.Xxl}";
                            }
                        }
                        
                        breakpoints.Append($"{prefix.Statement}\n");
                        breakpoints.Append($"    font-size: {fontSize};\n");                
                        breakpoints.Append("}\n");
                    }
                    
                    sb.Replace("$media-breakpoint-font-sizes;", breakpoints.ToString());
                }

                catch
                {
                    sb.Replace("$media-breakpoint-font-sizes;", string.Empty);
                }
            }

            else
            {
                sb.Replace("$media-breakpoint-font-sizes;", string.Empty);

                try
                {
                    foreach (var prefix in appState.MediaQueryPrefixes.Where(p => p.PrefixType.Equals("breakpoint", StringComparison.OrdinalIgnoreCase) && p.Prefix.Length > 3).OrderBy(o => o.PrefixOrder))
                    {
                        var fontSize = appState.Settings.Theme.FontSizeUnit?.GetType()
                            .GetProperty(prefix.Prefix.ToSentenceCase())
                            ?.GetValue(appState.Settings.Theme.FontSizeUnit, null);
                        
                        breakpoints.Append($"{prefix.Statement}\n");
                        breakpoints.Append($"    font-size: {fontSize};\n");                
                        breakpoints.Append("}\n");
                    }

                    sb.Replace("$adaptive-breakpoint-font-sizes;", breakpoints.ToString());
                }

                catch
                {
                    sb.Replace("$adaptive-breakpoint-font-sizes;", string.Empty);
                }
            }
        }        

        finally
        {
            appState.StringBuilderPool.Return(breakpoints);
        }
    }
    
	#endregion
	
	#region SCSS Transpiling
	
	/// <summary>
	/// Transpile SCSS markup into CSS.
	/// </summary>
	/// <param name="filePath"></param>
	/// <param name="rawScss"></param>
	/// <param name="runner"></param>
	/// <param name="showOutput"></param>
	/// <returns>Generated CSS file</returns>
	public static async Task<string> TranspileScssAsync(string filePath, string rawScss, SfumatoRunner runner, bool showOutput = true)
	{
		var sb = runner.AppState.StringBuilderPool.Get();
		var scss = runner.AppState.StringBuilderPool.Get();
        var styles = runner.AppState.StringBuilderPool.Get();
        var details = runner.AppState.StringBuilderPool.Get();

		try
		{
			if (string.IsNullOrEmpty(rawScss))
				rawScss = await Storage.ReadAllTextWithRetriesAsync(filePath, SfumatoAppState.FileAccessRetryMs);

			if (string.IsNullOrEmpty(rawScss))
				return string.Empty;
			
			var arguments = new List<string>();
			var cssOutputPath = filePath.TrimEnd(".scss") + ".css"; 
			var cssMapOutputPath = cssOutputPath + ".map";
			var includesBase = false;
			var includesUtilities = false;
			var includesShared = false;
			var timer = new Stopwatch();

			timer.Start();

			if (File.Exists(cssMapOutputPath))
				File.Delete(cssMapOutputPath);

			if (runner.AppState.Minify == false)
			{
				arguments.Add("--style=expanded");
				arguments.Add("--embed-sources");
			}

			else
			{
				arguments.Add("--style=compressed");
				arguments.Add("--no-source-map");
			}

			if (filePath.Contains(Path.DirectorySeparatorChar))
				arguments.Add($"--load-path={filePath[..filePath.LastIndexOf(Path.DirectorySeparatorChar)]}");
			else
				arguments.Add($"--load-path={runner.AppState.WorkingPath}");
			
			arguments.Add("--stdin");
			arguments.Add(cssOutputPath);
			
			#region Process @sfumato directives
			
			var matches = runner.AppState.SfumatoScssRegex.Matches(rawScss);
			var startIndex = 0;

			while (matches.Count > 0)
			{
				var match = matches[0];
				
				if (match.Index + match.Value.Length > startIndex)
					startIndex = match.Index + match.Value.Length;

				var matchValue = match.Value.CompactCss().TrimEnd(';');
				
				if (matchValue.EndsWith(" shared"))
				{
					rawScss = rawScss.Remove(match.Index, match.Value.Length);
					rawScss = rawScss.Insert(match.Index, runner.AppState.ScssSharedInjectable.ToString());
					includesShared = true;
				}

				else if (matchValue.EndsWith(" base"))
				{
					rawScss = rawScss.Remove(match.Index, match.Value.Length);
					rawScss = rawScss.Insert(match.Index, runner.AppState.ScssBaseInjectable.ToString());
					includesBase = true;
				}
				
				else if (matchValue.EndsWith(" utilities"))
				{
					var preamble = $"{Environment.NewLine}{Environment.NewLine}/* SFUMATO UTILITY CLASSES */{Environment.NewLine}{Environment.NewLine}";

					var utilitiesScss = runner.GenerateUtilityScss();
					
					rawScss = rawScss.Remove(match.Index, match.Value.Length);
					rawScss = rawScss.Insert(match.Index, preamble + utilitiesScss);

					includesUtilities = true;
				}
				
				matches = runner.AppState.SfumatoScssRegex.Matches(rawScss);
			}
			
			#endregion
			
			#region Process @apply directives
			
			matches = runner.AppState.SfumatoScssApplyRegex.Matches(rawScss);
			startIndex = 0;

			while (matches.Count > 0)
			{
				var match = matches[0];
				
				if (match.Index + match.Value.Length > startIndex)
					startIndex = match.Index + match.Value.Length;

				var matchValue = match.Value.Trim().TrimEnd(';').CompactCss().TrimStart("@apply ");

				var classes = (matchValue?.Split(' ') ?? Array.Empty<string>()).ToList();

				foreach (var selector in classes.ToList())
				{
					if (runner.AppState.IsValidCoreClassSelector(selector) == false)
						classes.Remove(selector);
				}

				if (classes.Count == 0)
				{
					rawScss = rawScss.Remove(match.Index, match.Value.Length);
				}
				
				else
				{
					styles.Clear();

					foreach (var selector in classes)
					{
						var newCssSelector = new CssSelector(runner.AppState, selector);

						await newCssSelector.ProcessSelectorAsync();

						if (newCssSelector.IsInvalid)
							continue;

						styles.Append(newCssSelector.GetStyles());
					}
					
					rawScss = rawScss.Remove(match.Index, match.Value.Length);
					rawScss = rawScss.Insert(match.Index, styles.ToString());
                }
				
				matches = runner.AppState.SfumatoScssApplyRegex.Matches(rawScss);
			}

			#endregion
			
			scss.Append(rawScss);
			
			var cmd = PipeSource.FromString(scss.ToString()) | Cli.Wrap(runner.AppState.SassCliPath)
				.WithArguments(args =>
				{
					foreach (var arg in arguments)
						args.Add(arg);

				})
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
				.WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb));

			await cmd.ExecuteAsync();

            sb.Clear();
            sb.Append(await Storage.ReadAllTextWithRetriesAsync(cssOutputPath, SfumatoAppState.FileAccessRetryMs));
            
            sb.Replace("html.theme-dark :root.", "html.theme-dark.");
            sb.Replace("html.theme-auto :root.", "html.theme-auto.");
            sb.Replace("html.theme-dark html.", "html.theme-dark.");
            sb.Replace("html.theme-auto html.", "html.theme-auto.");

            sb.Replace("html.theme-dark :root", "html.theme-dark");
            sb.Replace("html.theme-auto :root", "html.theme-auto");
            sb.Replace("html.theme-dark html", "html.theme-dark");
            sb.Replace("html.theme-auto html", "html.theme-auto");
            
            await File.WriteAllTextAsync(cssOutputPath, sb.ToString());

			if (showOutput == false)
				return sb.ToString();
			
			details.Clear();
				
			if (includesBase)
				details.Append(", +base");
				
			if (includesUtilities)
				details.Append($", +{runner.AppState.UsedClasses.Count(u => u.Value.IsInvalid == false):N0} utilities");

			if (includesShared)
				details.Append(", +shared");

			if (runner.AppState.Minify)
				details.Append(", minified");
				
			await Console.Out.WriteLineAsync($"{Strings.TriangleRight} Generated {SfumatoRunner.ShortenPathForOutput(filePath.TrimEnd(".scss", StringComparison.OrdinalIgnoreCase) + ".css", runner.AppState)} ({sb.Length.FormatBytes()}{details}) in {timer.FormatTimer()}");
            
			return sb.ToString();
		}

		catch
		{
			var error = sb.ToString();

			if (error.IndexOf($"Command:{Environment.NewLine}", StringComparison.OrdinalIgnoreCase) > -1)
			{
				error = error[..error.IndexOf($"Command:{Environment.NewLine}", StringComparison.OrdinalIgnoreCase)].Trim();
			}
			
			await Console.Out.WriteLineAsync($"{Strings.TriangleRight} {SfumatoRunner.ShortenPathForOutput(filePath, runner.AppState)} => {error}");

			return string.Empty;
		}
        
        finally
        {
            runner.AppState.StringBuilderPool.Return(sb);
            runner.AppState.StringBuilderPool.Return(scss);
            runner.AppState.StringBuilderPool.Return(styles);
            runner.AppState.StringBuilderPool.Return(details);
        }
	}
	
	#endregion
}

public class CssMediaQuery
{
	public int PrefixOrder { get; set; }
	public int Priority { get; set; }
	public string Prefix { get; set; } = string.Empty;
	public string PrefixType { get; set; } = string.Empty;
	public string Statement { get; set; } = string.Empty;
}