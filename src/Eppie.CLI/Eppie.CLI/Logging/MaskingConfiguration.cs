// ---------------------------------------------------------------------------- //
//                                                                              //
//   Copyright 2024 Eppie (https://eppie.io)                                    //
//                                                                              //
//   Licensed under the Apache License, Version 2.0 (the "License"),            //
//   you may not use this file except in compliance with the License.           //
//   You may obtain a copy of the License at                                    //
//                                                                              //
//       http://www.apache.org/licenses/LICENSE-2.0                             //
//                                                                              //
//   Unless required by applicable law or agreed to in writing, software        //
//   distributed under the License is distributed on an "AS IS" BASIS,          //
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.   //
//   See the License for the specific language governing permissions and        //
//   limitations under the License.                                             //
//                                                                              //
// ---------------------------------------------------------------------------- //

using System.Collections.ObjectModel;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Eppie.CLI.Logging
{
    /// <summary>
    /// Options for configuring masking in Serilog
    /// </summary>
    public class MaskingOptions
    {
        /// <summary>
        /// List of masking operators to apply to log entries
        /// </summary>
        public Collection<IMaskingOperator> MaskingOperators { get; } = [];
    }

    /// <summary>
    /// Serilog enricher that applies masking operators to log entries
    /// </summary>
    public class MaskingEnricher(Collection<IMaskingOperator> maskingOperators) : ILogEventEnricher
    {
        private readonly Collection<IMaskingOperator> _maskingOperators = maskingOperators ?? [];

        /// <summary>
        /// Enrich the log event with masking
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            ArgumentNullException.ThrowIfNull(logEvent);
            ArgumentNullException.ThrowIfNull(propertyFactory);

            if (_maskingOperators.Count == 0 || logEvent.Exception == null)
            {
                return;
            }

            // Apply masking to exception message and other properties
            // This is a simplified implementation for demonstration
            foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties.ToList())
            {
                if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
                {
                    string maskedValue = stringValue;
                    foreach (IMaskingOperator maskingOperator in _maskingOperators)
                    {
                        maskedValue = maskingOperator.Mask(maskedValue);
                    }

                    if (maskedValue != stringValue)
                    {
                        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, maskedValue));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for configuring masking in Serilog
    /// </summary>
    public static class MaskingConfigurationExtensions
    {
        /// <summary>
        /// Configure masking options for the logger
        /// </summary>
        public static LoggerConfiguration WithMasking(this LoggerEnrichmentConfiguration enrichConfiguration, MaskingOptions options)
        {
            ArgumentNullException.ThrowIfNull(enrichConfiguration);

            return options?.MaskingOperators?.Count > 0
                ? enrichConfiguration.With(new MaskingEnricher(options.MaskingOperators))
                : enrichConfiguration.FromLogContext();
        }
    }
}