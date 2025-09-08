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

using Eppie.CLI.Logging;

namespace Eppie.CLI.Tests
{
    /// <summary>
    /// Simple test to demonstrate masking functionality
    /// </summary>
    internal static class MaskingTest
    {
        /// <summary>
        /// Test the masking operators
        /// </summary>
        internal static void TestMasking()
        {
            Console.WriteLine("Testing Masking Operators:");
            
            EmailAddressMaskingOperator emailMasker = new EmailAddressMaskingOperator();
            string email = "user@example.com";
            string maskedEmail = emailMasker.Mask(email);
            Console.WriteLine($"Email: {email} -> {maskedEmail}");
            
            IbanMaskingOperator ibanMasker = new IbanMaskingOperator();
            string iban = "GB82WEST12345698765432";
            string maskedIban = ibanMasker.Mask(iban);
            Console.WriteLine($"IBAN: {iban} -> {maskedIban}");
            
            CreditCardMaskingOperator cardMasker = new CreditCardMaskingOperator();
            string card = "4111-1111-1111-1111";
            string maskedCard = cardMasker.Mask(card);
            Console.WriteLine($"Card: {card} -> {maskedCard}");
        }
    }
}