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

namespace Eppie.CLI.Logging
{
    /// <summary>
    /// Interface for masking operators that can transform sensitive data
    /// </summary>
    public interface IMaskingOperator
    {
        /// <summary>
        /// Mask the input value
        /// </summary>
        string Mask(string input);
    }

    /// <summary>
    /// Hash transform operator that applies hashing to sensitive data
    /// </summary>
    public class HashTransformOperator<T> : IMaskingOperator where T : IMaskingOperator, new()
    {
        private readonly T _innerOperator = new();

        /// <summary>
        /// Mask the input value using the inner operator
        /// </summary>
        public string Mask(string input)
        {
            return _innerOperator.Mask(input);
        }
    }

    /// <summary>
    /// Masking operator for email addresses
    /// </summary>
    public class EmailAddressMaskingOperator : IMaskingOperator
    {
        /// <summary>
        /// Mask the email address
        /// </summary>
        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains('@', StringComparison.Ordinal))
            {
                return input;
            }

            string[] parts = input.Split('@');
            if (parts.Length != 2)
            {
                return input;
            }

            string localPart = parts[0];
            string domain = parts[1];

            // Mask local part, keeping first and last character if length > 2
            string maskedLocal = localPart.Length switch
            {
                <= 2 => new string('*', localPart.Length),
                _ => $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}"
            };

            return $"{maskedLocal}@{domain}";
        }
    }

    /// <summary>
    /// Masking operator for IBAN numbers
    /// </summary>
    public class IbanMaskingOperator : IMaskingOperator
    {
        /// <summary>
        /// Mask the IBAN number
        /// </summary>
        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 4)
            {
                return input;
            }

            // Keep first 4 characters (country code and check digits) and mask the rest
            return $"{input[..4]}{new string('*', Math.Max(0, input.Length - 4))}";
        }
    }

    /// <summary>
    /// Masking operator for credit card numbers
    /// </summary>
    public class CreditCardMaskingOperator : IMaskingOperator
    {
        /// <summary>
        /// Mask the credit card number
        /// </summary>
        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Remove spaces and dashes for processing
            string cleanInput = input.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);

            if (cleanInput.Length < 8)
            {
                return input;
            }

            // Mask all but last 4 digits
            string masked = new string('*', cleanInput.Length - 4) + cleanInput[^4..];

            // Preserve original formatting by replacing digits with masked version
            string result = input;
            int maskIndex = 0;

            for (int i = 0; i < result.Length && maskIndex < masked.Length; i++)
            {
                if (char.IsDigit(result[i]))
                {
                    result = result.Remove(i, 1).Insert(i, masked[maskIndex].ToString());
                    maskIndex++;
                }
            }

            return result;
        }
    }
}