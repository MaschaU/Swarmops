using System;
using System.Collections.Generic;
using Swarmops.Basic.Types;
using Swarmops.Basic.Types.Financial;
using Swarmops.Common.Enums;
using Swarmops.Database;
using Swarmops.Logic.Communications;
using Swarmops.Logic.Communications.Payload;
using Swarmops.Logic.Structure;
using Swarmops.Logic.Support;
using Swarmops.Logic.Support.LogEntries;
using Swarmops.Logic.Swarm;

namespace Swarmops.Logic.Financial
{
    public class ExpenseClaim : BasicExpenseClaim, IValidatable, IApprovable, IPayable
    {
        #region Construction and Creation

        private ExpenseClaim() : base (null)
        {
        } // Private constructor prevents wanton creation

        private ExpenseClaim (BasicExpenseClaim basic) : base (basic)
        {
        } // Used by FromBasic()

        public static ExpenseClaim FromBasic (BasicExpenseClaim basic)
        {
            return new ExpenseClaim (basic);
        }

        public static ExpenseClaim FromIdentity (int expenseId)
        {
            return FromBasic (SwarmDb.GetDatabaseForReading().GetExpenseClaim (expenseId));
        }

        public static ExpenseClaim FromIdentityAggressive (int expenseId)
        {
            return FromBasic (SwarmDb.GetDatabaseForWriting().GetExpenseClaim (expenseId));
            // ForWriting is intentional - bypass replication lag
        }

        public static ExpenseClaim Create (Person claimer, Organization organization, FinancialAccount budget,
            DateTime expenseDate, string description, Int64 amountCents, Int64 vatCents, ExpenseClaimGroup group = null)
        {
            ExpenseClaim newClaim =
                FromIdentityAggressive (SwarmDb.GetDatabaseForWriting()
                    .CreateExpenseClaim (claimer.Identity, organization?.Identity ?? 0,
                        budget?.Identity ?? 0, expenseDate, description, amountCents)); // budget can be 0 initially if created with a group

            if (vatCents > 0)
            {
                newClaim.VatCents = vatCents;
            }

            if (group != null)
            {
                newClaim.Group = group;
            }

            if (budget != null && organization != null)
            {
                // Create the financial transaction with rows

                string transactionDescription = "Expense #" + newClaim.OrganizationSequenceId + ": " + description;
                    // TODO: Localize

                if (transactionDescription.Length > 64)
                {
                    transactionDescription = transactionDescription.Substring(0, 61) + "...";
                }


                DateTime expenseTxDate = expenseDate;
                int ledgersClosedUntil = organization.Parameters.FiscalBooksClosedUntilYear;

                if (ledgersClosedUntil >= expenseDate.Year)
                {
                    expenseTxDate = DateTime.UtcNow; // If ledgers are closed for the actual expense time, account now
                }

                FinancialTransaction transaction =
                    FinancialTransaction.Create(organization.Identity, expenseTxDate,
                        transactionDescription);

                transaction.AddRow(organization.FinancialAccounts.DebtsExpenseClaims, -amountCents, claimer);
                if (vatCents > 0)
                {
                    transaction.AddRow(budget, amountCents - vatCents, claimer);
                    transaction.AddRow(organization.FinancialAccounts.AssetsVatInboundUnreported, vatCents, claimer);
                }
                else
                {
                    transaction.AddRow(budget, amountCents, claimer);
                }

                // Make the transaction dependent on the expense claim

                transaction.Dependency = newClaim;

                // Create notifications

                OutboundComm.CreateNotificationApprovalNeeded(budget, claimer, string.Empty,
                    newClaim.BudgetAmountCents/100.0,
                    description, NotificationResource.ExpenseClaim_Created);
                    // Slightly misplaced logic, but failsafer here
                OutboundComm.CreateNotificationFinancialValidationNeeded(organization, newClaim.AmountCents/100.0,
                    NotificationResource.Receipts_Filed);
                SwarmopsLogEntry.Create(claimer,
                    new ExpenseClaimFiledLogEntry(claimer /*filing person*/, claimer /*beneficiary*/,
                        newClaim.BudgetAmountCents/100.0,
                        vatCents/100.0, budget, description), newClaim);

                // Clear a cache
                FinancialAccount.ClearApprovalAdjustmentsCache(organization);
            }

            return newClaim;
        }

        #endregion



        public new int OrganizationSequenceId
        {
            get
            {
                if (base.OrganizationSequenceId == 0)
                {
                    // This case is for legacy installations before DbVersion 66, when
                    // OrganizationSequenceId was added for each new expense claim

                    SwarmDb db = SwarmDb.GetDatabaseForWriting();
                    base.OrganizationSequenceId = db.SetExpenseClaimSequence(this.Identity);
                    return base.OrganizationSequenceId;
                }

                return base.OrganizationSequenceId;
            }
        }

        public Person Claimer
        {
            get { return Person.FromIdentity (base.ClaimingPersonId); }
        }

        public bool Approved
        {
            get { return Validated && Attested; }
        }


        public new Int64 VatCents
        {
            get { return base.VatCents; }
            set
            {
                base.VatCents = value;
                SwarmDb.GetDatabaseForWriting().SetExpenseClaimVatCents(this.Identity, value);
            }
        }

        public Int64 BudgetAmountCents
        {
            get { return this.AmountCents - this.VatCents; }
        }


        public string ClaimerCanonical
        {
            get
            {
                try
                {
                    return Claimer.Canonical;
                }
                catch (ArgumentException)
                {
                    return "NOT-IN-DATABASE"; // For development purposes only!
                }
            }
        }

        public Organization Organization
        {
            get { return Organization.FromIdentity (OrganizationId); }
        }

        public ExpenseClaimGroup Group
        {
            get
            {
                if (this.GroupId == 0)
                {
                    return null;
                }

                return ExpenseClaimGroup.FromIdentity(this.GroupId);
            }
            set
            {
                int newGroupId = value?.Identity ?? 0;

                if (this.GroupId != newGroupId)
                {
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimGroup(this.Identity, newGroupId);
                }
            }
        }

        public FinancialTransaction FinancialTransaction
        {
            get
            {
                FinancialTransactions transactions = FinancialTransactions.ForDependentObject (this);

                if (transactions.Count == 0)
                {
                    return null; // Only for grouped transactions that are still under construction
                }

                if (transactions.Count == 1)
                {
                    return transactions[0];
                }

                throw new InvalidOperationException ("It appears expense claim #" + Identity +
                                                     " has multiple dependent financial transactions. This is an invalid state.");
            }
        }

        public new bool Attested
        {
            get { return base.Attested; }
            set
            {
                if (base.Attested != value)
                {
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimAttested (Identity, value);
                    base.Attested = value;
                }
            }
        }

        public new bool Validated
        {
            get { return base.Validated; }
            set
            {
                if (base.Validated != value)
                {
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimValidated (Identity, value);
                    base.Validated = value;
                }
            }
        }

        public new bool Open
        {
            get { return base.Open; }
            set
            {
                if (base.Open != value)
                {
                    base.Open = value;
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimOpen (Identity, value);
                }
            }
        }

        public new bool Claimed
        {
            get { return base.Claimed; }
            set
            {
                if (base.Claimed != value)
                {
                    base.Claimed = value;
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimClaimed (Identity, value);
                    UpdateFinancialTransaction (Claimer);
                }
            }
        }

        public new bool Repaid
        {
            get { return base.Repaid; }
            set
            {
                if (base.Repaid != value)
                {
                    base.Repaid = value;
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimRepaid (Identity, value);
                }
            }
        }

        public new bool KeepSeparate
        {
            get { return base.KeepSeparate; }
            set
            {
                if (base.KeepSeparate != value)
                {
                    base.KeepSeparate = value;
                    SwarmDb.GetDatabaseForWriting().SetExpenseClaimKeepSeparate (Identity, value);
                }
            }
        }

        public new string Description
        {
            get { return base.Description; }
            set
            {
                SwarmDb.GetDatabaseForWriting().SetExpenseClaimDescription (Identity, value);
                base.Description = value;
            }
        }

        public int BudgetYear
        {
            get { return CreatedDateTime.Year; }
            set
            {
                // ignore
            }
        }

        public decimal Amount
        {
            get { return base.AmountCents/100.0m; }
        }

        public new Int64 AmountCents
        {
            get { return base.AmountCents; }
        }

        public new DateTime ExpenseDate
        {
            get { return base.ExpenseDate; }
            set
            {
                // TODO
            }
        }


        public Documents Documents
        {
            get { return Documents.ForObject (this); }
        }


        public FinancialValidations Validations
        {
            get { return FinancialValidations.ForObject (this); }
        }

        public Payout Payout
        {
            get
            {
                if (Open)
                {
                    return null; // or throw?
                }

                int payoutId =
                    SwarmDb.GetDatabaseForReading().GetPayoutIdFromDependency (this);

                if (payoutId == 0)
                {
                    return null; // or throw? When expense claim system was being phased in, payouts did not exist
                }

                return Payout.FromIdentity (payoutId);
            }
        }

        #region IValidatable Members

        public void Validate (Person validator)
        {
            SwarmDb.GetDatabaseForWriting().SetExpenseClaimValidated (Identity, true);
            SwarmDb.GetDatabaseForWriting().CreateFinancialValidation (FinancialValidationType.Validation,
                FinancialDependencyType.ExpenseClaim, Identity,
                DateTime.UtcNow, validator.Identity, (double) Amount);
            base.Validated = true;

            OutboundComm.CreateNotificationOfFinancialValidation (Budget, Claimer, AmountCents/100.0, Description,
                NotificationResource.ExpenseClaim_Validated);
        }

        public void RetractValidation (Person retractor)
        {
            SwarmDb.GetDatabaseForWriting().SetExpenseClaimValidated (Identity, false);
            SwarmDb.GetDatabaseForWriting().CreateFinancialValidation (FinancialValidationType.UndoValidation,
                FinancialDependencyType.ExpenseClaim, Identity,
                DateTime.UtcNow, retractor.Identity, (double) Amount);
            base.Validated = false;

            OutboundComm.CreateNotificationOfFinancialValidation (Budget, Claimer, AmountCents/100.0, Description,
                NotificationResource.ExpenseClaim_ValidationRetracted);
        }

        #endregion

        #region IApprovable Members

        public void Approve (Person approvingPerson)
        {
            Attested = true;
            SwarmDb.GetDatabaseForWriting().CreateFinancialValidation(FinancialValidationType.Approval,
                FinancialDependencyType.ExpenseClaim, Identity,
                DateTime.UtcNow, approvingPerson.Identity, (double) Amount);

            OutboundComm.CreateNotificationOfFinancialValidation (Budget, Claimer, AmountCents/100.0, Description,
                NotificationResource.ExpenseClaim_Approved);

            UpdateFinancialTransaction(approvingPerson); // will re-enable tx if it was zeroed out earlier
        }

        public void RetractApproval (Person retractingPerson)
        {
            Attested = false;
            SwarmDb.GetDatabaseForWriting().CreateFinancialValidation(FinancialValidationType.UndoApproval,
                FinancialDependencyType.ExpenseClaim, Identity,
                DateTime.UtcNow, retractingPerson.Identity, (double) Amount);

            OutboundComm.CreateNotificationOfFinancialValidation (Budget, Claimer, AmountCents/100.0, Description,
                NotificationResource.ExpenseClaim_ApprovalRetracted);
        }

        public void DenyApproval (Person denyingPerson, string reason)
        {
            Attested = false;
            Open = false;

            SwarmDb.GetDatabaseForWriting().CreateFinancialValidation(FinancialValidationType.Kill,
                FinancialDependencyType.ExpenseClaim, Identity,
                DateTime.UtcNow, denyingPerson.Identity, (double)Amount);

            OutboundComm.CreateNotificationOfFinancialValidation(Budget, Claimer, AmountCents / 100.0, Description,
                NotificationResource.ExpenseClaim_Denied, reason);

            UpdateFinancialTransaction(denyingPerson); // will zero out transaction
        }

        #endregion

        public FinancialAccount Budget
        {
            get { return FinancialAccount.FromIdentity (base.BudgetId); }
        }

        [Obsolete ("Obsolete", true)]
        public void CreateEvent (ExpenseEventType eventType, Person person)
        {
            // OBSOLETE

            // CreateEvent (eventType, person.Identity);
        }

        public void CreateEvent (ExpenseEventType eventType, int personId)
        {
            // OBSOLETE

            // SwarmDb.GetDatabaseForWriting().CreateExpenseEvent (Identity, eventType, personId);

            // TODO: Repopulate Events property, when created
        }

        public void Close()
        {
            Open = false;
        }

        public void SetAmountCents (Int64 amountCents, Person settingPerson)
        {
            if (base.AmountCents == amountCents)
            {
                return;
            }

            base.AmountCents = amountCents;
            SwarmDb.GetDatabaseForWriting().SetExpenseClaimAmount (Identity, amountCents);
            UpdateFinancialTransaction (settingPerson);

            if (Validated)
            {
                // Reset validation, since amount was changed
                RetractValidation (settingPerson);
            }
        }


        public void SetBudget (FinancialAccount budget, Person settingPerson)
        {
            SwarmDb.GetDatabaseForWriting().SetExpenseClaimBudget (Identity, budget.Identity);
            base.BudgetId = budget.Identity;
            UpdateFinancialTransaction (settingPerson);
        }


        public void Kill (Person killingPerson)
        {
            // Set the state to Closed, Unvalidated, Unattested

            Attested = false;
            Validated = false;
            Open = false;

            UpdateFinancialTransaction (killingPerson);
            // will zero out transaction since both Validated and Open are false

            // Mark transaction as invalid in description

            FinancialTransaction.Description = "[strikeout]Expense Claim #" + Identity;
        }


        public void Recalibrate()
        {
            UpdateFinancialTransaction (null); // only to be used for fix-bookkeeping scripts
        }


        private void UpdateFinancialTransaction (Person updatingPerson)
        {
            Dictionary<int, Int64> nominalTransaction = new Dictionary<int, Int64>();

            int debtAccountId = Organization.FinancialAccounts.DebtsExpenseClaims.Identity;

            if (!Claimed)
            {
                debtAccountId = Organization.FinancialAccounts.CostsAllocatedFunds.Identity;
            }

            if (Validated || Open)
            {
                // ...only holds values if not closed as invalid...

                nominalTransaction[debtAccountId] = -AmountCents;

                if (this.Organization.VatEnabled)
                {
                    nominalTransaction[BudgetId] = AmountCents - VatCents;
                    nominalTransaction[Organization.FinancialAccounts.AssetsVatInboundUnreported.Identity] = VatCents;
                }
                else
                {
                    nominalTransaction[BudgetId] = AmountCents;
                }
            }

            FinancialTransaction.RecalculateTransaction (nominalTransaction, updatingPerson);
        }

        public bool PaidOut // IPayable naming convention
        {
            get { return this.Repaid; }
            set { this.Repaid = value;  }
        }
    }
}