namespace SvgToIco

open System
open Microsoft.Extensions.Configuration

module Configuration=
    
    type SvgToIcoConfiguration = {        
        LogFile:string
        LogLevel:string
    }
    
    let getConfiguration () =
        // Build configuration
        let configurationBuilder = 
            (ConfigurationBuilder())
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
                .Build()
        
        // Get the MySettings section
        let mySettingsSection = configurationBuilder.GetSection("SvgToIco")
        
        // Bind the section to a strongly-typed object
        let configuration = mySettingsSection.Get<SvgToIcoConfiguration>()
        let expandandedConfiguration = { configuration with                                            
                                            LogFile =  System.Environment.ExpandEnvironmentVariables(configuration.LogFile) }
        expandandedConfiguration

