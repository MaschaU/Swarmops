﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Swarmops.Basic.Types.Financial;
using Swarmops.Common.Enums;
using Swarmops.Database;
using Swarmops.Logic.Structure;

namespace Swarmops.Logic.Financial
{
    public class HotBitcoinAddress: BasicHotBitcoinAddress
    {
        private HotBitcoinAddress(BasicHotBitcoinAddress basic): base (basic)
        {
            // private ctor
        }

        public static HotBitcoinAddress FromBasic (BasicHotBitcoinAddress basic)
        {
            return new HotBitcoinAddress (basic); // invoke private ctor
        }

        public static HotBitcoinAddress FromIdentity(int hotBitcoinAddressId)
        {
            return FromBasic(SwarmDb.GetDatabaseForReading().GetHotBitcoinAddress(hotBitcoinAddressId));
        }

        internal static HotBitcoinAddress FromIdentityAggressive(int hotBitcoinAddressId)
        {
            return FromBasic(SwarmDb.GetDatabaseForWriting().GetHotBitcoinAddress(hotBitcoinAddressId)); // "Writing" is intentional
        }

        public static HotBitcoinAddress Create(Organization organization, string derivationPath)
        {


/*
            string bitcoinAddress =
                FinancialAccounts.BitcoinHotPublicRoot
                    .Derive((uint)this.CurrentOrganization.Identity)
                    .Derive(BitcoinUtility.BitcoinDonationsIndex)
                    .Derive((uint)this.CurrentUser.Identity)
                    .PubKey.GetAddress(Network.Main)
                    .ToString();*/

            throw new NotImplementedException();
        }

        public static HotBitcoinAddress Create(Organization organization, BitcoinChain chain, params int[] derivationPath)
        {
            ExtPubKey extPubKey = BitcoinUtility.BitcoinHotPublicRoot;
            extPubKey = extPubKey.Derive((uint)organization.Identity);
            string derivationPathString = string.Empty;

            foreach (int derivation in derivationPath) // requires that order is consistent from 0 to n-1
            {
                extPubKey = extPubKey.Derive((uint)derivation);
                derivationPathString += " " + derivation.ToString(CultureInfo.InvariantCulture);
            }

            derivationPathString = derivationPathString.TrimStart();
            string bitcoinAddress = extPubKey.PubKey.GetAddress(Network.Main).ToString();    // TODO: CHANGE NETWORK.MAIN TO NEW LOOKUP
            // string bitcoinAddressFallback = extPubKey.PubKey.GetAddress(Network.Main).ToString(); // The fallback address would be the main address

            int hotBitcoinAddressId =
                SwarmDb.GetDatabaseForWriting()
                    .CreateHotBitcoinAddressConditional(organization.Identity, chain, derivationPathString, bitcoinAddress);

            return FromIdentityAggressive(hotBitcoinAddressId);
        }

        public static HotBitcoinAddress CreateUnique (Organization organization, BitcoinChain chain, params int[] derivationPath)
        {
            ExtPubKey extPubKey = BitcoinUtility.BitcoinHotPublicRoot;
            extPubKey = extPubKey.Derive((uint)organization.Identity);
            string derivationPathString = string.Empty;

            foreach (int derivation in derivationPath) // requires that order is consistent from 0 to n-1
            {
                extPubKey = extPubKey.Derive((uint)derivation);
                derivationPathString += " " + derivation.ToString(CultureInfo.InvariantCulture);
            }

            derivationPathString = derivationPathString.TrimStart();

            int hotBitcoinAddressId =
                SwarmDb.GetDatabaseForWriting()
                    .CreateHotBitcoinAddress(organization.Identity, chain, derivationPathString);

            HotBitcoinAddress addressTemp = FromIdentityAggressive(hotBitcoinAddressId);

            // Derive the last step with the now-assigned unique identifier, then set address, read again, and return

            extPubKey = extPubKey.Derive((uint) addressTemp.UniqueDerive);

            string bitcoinAddressString = extPubKey.PubKey.GetAddress(Network.Main).ToString();

            SwarmDb.GetDatabaseForWriting().SetHotBitcoinAddressAddress(hotBitcoinAddressId, bitcoinAddressString);

            return FromIdentityAggressive(hotBitcoinAddressId);
        }

        public static HotBitcoinAddress FromAddress (BitcoinChain chain, string bitcoinAddress)
        {
            return FromBasic(SwarmDb.GetDatabaseForReading().GetHotBitcoinAddress(chain, bitcoinAddress));
        }

        public static HotBitcoinAddress GetAddressOrForkCore(BitcoinChain chain, string bitcoinAddress)
        {
            try
            {
                return FromBasic(SwarmDb.GetDatabaseForReading().GetHotBitcoinAddress(chain, bitcoinAddress));
            }
            catch (Exception)
            {
                // Forked address off of Core is not registered yet

                HotBitcoinAddress coreAddress =
                    FromBasic(SwarmDb.GetDatabaseForReading().GetHotBitcoinAddress(BitcoinChain.Core, bitcoinAddress));

                return Create(coreAddress.Organization, chain, coreAddress.DerivationPathIntArray);
            }
        }

        public static HotBitcoinAddress OrganizationWalletZero (Organization organization, BitcoinChain chain)
        {
            return Create (organization, chain, BitcoinUtility.BitcoinWalletIndex, 0);
        }

        public BitcoinSecret PrivateKey
        {
            get
            {
                // This will throw if the private root key is not available to the running code, and that's as designed

                ExtKey secretExtKey = BitcoinUtility.BitcoinHotPrivateRoot.Derive ((uint) base.OrganizationId);

                foreach (string derivationString in DerivationPath.Split (' '))
                {
                    if (!String.IsNullOrEmpty(derivationString.Trim()))
                    {
                        secretExtKey = secretExtKey.Derive(UInt32.Parse(derivationString.Trim()));
                    }
                }

                if (this.UniqueDerive > -1)
                {
                    secretExtKey = secretExtKey.Derive((uint) this.UniqueDerive);
                }

                return secretExtKey.PrivateKey.GetBitcoinSecret (Network.Main);
            }
        }

        public Organization Organization
        {
            get { return Organization.FromIdentity (base.OrganizationId); }
        }

        public HotBitcoinAddressUnspents Unspents
        {
            get { return HotBitcoinAddressUnspents.ForAddress (this); }
        }

        public Int64 UnspentSatoshis
        {
            get
            {
                HotBitcoinAddressUnspents unspents = Unspents;

                return unspents.Sum (unspent => unspent.AmountSatoshis);
            }
        }

        public int[] DerivationPathIntArray
        {
            get
            {
                List<int> result = new List<int>();
                foreach (string number in DerivationPath.Split(' '))
                {
                    result.Add(Int32.Parse(number));
                }

                return result.ToArray();
            }
        }

        public string HumanAddress
        {
            get
            {
                bool dummy1, dummy2;

                return Swarmops.Logic.Support.BitcoinCashAddressConversion.LegacyAddressToCashAddress(this.MachineAddress, out dummy1, out dummy2);
            }
        }

        public string ProtocolLevelAddress
        {
            get { return base.MachineAddress; }
        }

        public void UpdateTotal()
        {
            SwarmDb.GetDatabaseForWriting().UpdateHotBitcoinAddressUnspentTotal(this.Identity);
        }
    }
}
