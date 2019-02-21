﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swarmops.Common.Enums
{
    public enum PaymentTargetField
    {
        Unknown = 0,
        Iban,
        Bban,
        BicSwift,
        GiroNumber,
        GiroService,
        ClearingNumber,
        AccountNumber,
        BankAccountHolderName,
        BankName,
        CashAccount,
        CryptoAddress
    }
}
