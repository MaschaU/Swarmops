using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Xml;
using Swarmops.Basic.Types.Financial;
using Swarmops.Common;

// This is the first part of Database to fully use MySql.

// TODO: This was written with several classes in one file. Break out to one class per file.
using Swarmops.Common.Enums;
using Swarmops.Common.Interfaces;

namespace Swarmops.Database
{
    public partial class SwarmDb
    {
        private const string financialAccountFieldSequence =
            " FinancialAccountId,Name,OrganizationId,AccountType,ParentFinancialAccountId," + // 0-4
            " OwnerPersonId,Open,OpenedYear,ClosedYear,Expensable," + // 5-9
            " Administrative,Active,LinkBackward,LinkForward " + // 10-13
            " FROM FinancialAccounts ";

        public const string financialTransactionFieldSequence =
            " FinancialTransactionId,OrganizationId,OrganizationSequenceId,DateTime,Comment," + // 0-4
            " ImportHash " + // 5
            " FROM FinancialTransactions ";

        public int CreateFinancialAccount (int pOrganizationId, string pName, FinancialAccountType pAccountType,
            int pParentFinancialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("CreateFinancialAccount", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "pOrganizationId", pOrganizationId);
                AddParameterWithName (command, "pName", pName);
                AddParameterWithName (command, "pAccountType", (int) pAccountType);
                AddParameterWithName (command, "pParentFinancialAccountId", pParentFinancialAccountId);

                return Convert.ToInt32 (command.ExecuteScalar());
            }
        }


        public BasicFinancialAccount GetFinancialAccount (int financialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialAccountFieldSequence + " WHERE FinancialAccountId=" +
                        financialAccountId + ";", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadFinancialAccountFromDataReader (reader);
                    }

                    throw new ArgumentException ("Unknown Account Id");
                }
            }
        }

        // This is WAAAAY too much repetition of code. Couldn't you create two generic functions with
        // this structure, call them GetDatabaseObject and GetDatabaseCollection, and pass the query,
        // the reader delegate, and the expected type?

        public BasicFinancialAccount[] GetFinancialAccountsAll()
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand(
                        "SELECT " + financialAccountFieldSequence + 
                        " ORDER BY AccountType, Name;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(ReadFinancialAccountFromDataReader(reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialAccount[] GetFinancialAccountsForOrganization(int organizationId)
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand(
                        "SELECT " + financialAccountFieldSequence + " WHERE OrganizationId=" +
                        organizationId + " ORDER BY AccountType, Name;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(ReadFinancialAccountFromDataReader(reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialAccount[] GetFinancialAccountsOwnedByPerson(int ownerPersonId)
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialAccountFieldSequence + " WHERE OwnerPersonId=" +
                        ownerPersonId + " ORDER BY Name;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialAccountFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialAccount[] GetFinancialAccounts (int[] financialAccountIds)
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialAccountFieldSequence + " WHERE FinancialAccountId IN (" +
                        JoinIds (financialAccountIds) + ") ORDER BY AccountType, Name;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialAccountFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialTransaction GetFinancialTransaction (int financialTransactionId)
        {
            BasicFinancialTransaction[] array = GetFinancialTransactions (new[] {financialTransactionId});

            if (array.Length == 0)
            {
                throw new ArgumentException ("No such FinancialTransactionId");
            }

            return array[0];
        }

        public BasicFinancialTransaction[] GetFinancialTransactions (int[] financialTransactionIds)
        {
            List<BasicFinancialTransaction> result = new List<BasicFinancialTransaction>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialTransactionFieldSequence + " WHERE FinancialTransactionId IN (" +
                        JoinIds (financialTransactionIds) + ");", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialTransactionFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialTransaction GetFinancialTransactionFromImportKey (int organizationId, string importKey)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialTransactionFieldSequence +  " WHERE OrganizationId=" +
                        organizationId + " AND ImportHash='" + SqlSanitize(importKey) + "';", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadFinancialTransactionFromDataReader (reader);
                    }

                    throw new ArgumentException ("No FinancialTransaction with supplied import key");
                }
            }
        }

        [Obsolete ("This method uses floating point for financials. Deprecated. Do not use.", true)]
        public double GetFinancialAccountBalanceTotal (int financialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT Sum(AmountCents) FROM FinancialTransactionRows WHERE FinancialAccountId=" +
                        financialAccountId + ";", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt64 (0)/100.0;
                    }

                    throw new ArgumentException ("Unknown Account Id");
                }
            }
        }

        public Int64 GetFinancialAccountBalanceTotalCents (int financialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT Sum(AmountCents) FROM FinancialTransactionRows WHERE FinancialAccountId=" +
                        financialAccountId + ";", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            return reader.GetInt64(0);
                        }
                    }

                    return 0; // Balance appears to be zero
                }
            }
        }

        public Int64 GetFinancialAccountBalanceDeltaCents (int financialAccountId, DateTime startDate, DateTime endDate)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select Sum(FinancialTransactionRows.AmountCents),Count(FinancialTransactions.FinancialTransactionId) from FinancialTransactionRows,FinancialTransactions Where FinancialTransactionRows.FinancialAccountId=" +
                        financialAccountId +
                        " AND FinancialTransactionRows.FinancialTransactionId=FinancialTransactions.FinancialTransactionId AND FinancialTransactionRows.Deleted=0 AND FinancialTransactions.DateTime >= '" +
                        XmlConvert.ToString(startDate, "yyyy-MM-dd HH:mm:ss") + "' AND FinancialTransactions.DateTime < '" +
                        XmlConvert.ToString(endDate, "yyyy-MM-dd HH:mm:ss") + "';", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader.IsDBNull (0))
                        {
                            // No rows, so no delta

                            return 0;
                        }

                        return reader.GetInt64 (0);
                    }

                    throw new ArgumentException ("Unknown Account Id");
                }
            }
        }

        public Int64 GetFinancialAccountBalanceDeltaCents (int[] financialAccountIds, DateTime startDate,
            DateTime endDate)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select Sum(FinancialTransactionRows.AmountCents),Count(FinancialTransactions.FinancialTransactionId) from FinancialTransactionRows,FinancialTransactions Where FinancialTransactionRows.FinancialAccountId IN (" +
                        JoinIds (financialAccountIds) +
                        ") AND FinancialTransactionRows.FinancialTransactionId=FinancialTransactions.FinancialTransactionId AND FinancialTransactionRows.Deleted=0 AND FinancialTransactions.DateTime >= '" +
                        startDate.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "' AND FinancialTransactions.DateTime < '" +
                        endDate.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "';", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader.IsDBNull (0))
                        {
                            // No rows, so no delta

                            return 0;
                        }

                        return reader.GetInt64 (0);
                    }

                    throw new ArgumentException ("Unknown Account Id");
                }
            }
        }

        [Obsolete ("This method uses floating point for financials. Deprecated. Do not use.")]
        public decimal GetFinancialAccountBalanceDelta (int financialAccountId, DateTime startDate, DateTime endDate)
        {
            return GetFinancialAccountBalanceDeltaCents (financialAccountId, startDate, endDate)/100.0m;
        }

        public BasicFinancialAccountRow[] GetFinancialAccountRows (int financialAccountId, DateTime startDateTime,
            DateTime endDateTime, bool selectFar)
        {
            return GetFinancialAccountRows (new[] {financialAccountId}, startDateTime, endDateTime, selectFar);
        }

        public BasicFinancialAccountRow[] GetFinancialAccountRows (int[] financialAccountIds, DateTime startDateTime,
            DateTime endDateTime, bool selectFar)
        {
            List<BasicFinancialAccountRow> result = new List<BasicFinancialAccountRow>();

            // initialize to near selector (equal sign by the lower bound

            string selectorLower = ">=";
            string selectorUpper = "<";

            if (selectFar) // equal sign by the upper bound
            {
                selectorLower = ">";
                selectorUpper = "<=";
            }

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select FinancialTransactionRows.FinancialAccountId,FinancialTransactionRows.FinancialTransactionId,FinancialTransactionRows.FinancialTransactionRowId,FinancialTransactions.DateTime,FinancialTransactions.Comment,FinancialTransactionRows.AmountCents,FinancialTransactionRows.CreatedDateTime,FinancialTransactionRows.CreatedByPersonId FROM FinancialTransactions,FinancialTransactionRows WHERE FinancialTransactionRows.Deleted=0 AND FinancialTransactions.FinancialTransactionId=FinancialTransactionRows.FinancialTransactionId AND FinancialTransactionRows.FinancialAccountId IN (" +
                        JoinIds (financialAccountIds) + ") AND DateTime " + selectorLower + " '" +
                        startDateTime.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                        "' AND DateTime " + selectorUpper + " '" + endDateTime.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                        "' ORDER BY DateTime,FinancialTransactions.FinancialTransactionId,FinancialTransactionRows.CreatedDateTime;",
                        connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialAccountRowFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }


        public BasicFinancialAccountRow[] GetLastFinancialAccountRows (int financialAccountId, int rowCount)
        {
            return GetLastFinancialAccountRows (new[] {financialAccountId}, rowCount);
        }

        public BasicFinancialAccountRow[] GetLastFinancialAccountRows (int[] financialAccountIds, int rowCount)
        {
            List<BasicFinancialAccountRow> result = new List<BasicFinancialAccountRow>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select FinancialTransactionRows.FinancialAccountId,FinancialTransactionRows.FinancialTransactionId,FinancialTransactionRows.FinancialTransactionRowId,FinancialTransactions.DateTime,FinancialTransactions.Comment,FinancialTransactionRows.AmountCents,FinancialTransactionRows.CreatedDateTime,FinancialTransactionRows.CreatedByPersonId FROM FinancialTransactions,FinancialTransactionRows WHERE FinancialTransactionRows.Deleted=0 AND FinancialTransactions.FinancialTransactionId=FinancialTransactionRows.FinancialTransactionId AND FinancialTransactionRows.FinancialAccountId IN (" +
                        JoinIds (financialAccountIds) +
                        ") ORDER BY DateTime DESC,FinancialTransactions.FinancialTransactionId,FinancialTransactionRows.CreatedDateTime LIMIT " +
                        rowCount + ";",
                        connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialAccountRowFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }


        public BasicFinancialTransactionRow[] GetFinancialTransactionRows (int financialTransactionId)
        {
            List<BasicFinancialTransactionRow> result = new List<BasicFinancialTransactionRow>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select FinancialTransactionRowId, FinancialAccountId," + financialTransactionId +
                        " AS FinancialTransactionId,AmountCents,CreatedDateTime,CreatedByPersonId FROM FinancialTransactionRows WHERE FinancialTransactionId=" +
                        financialTransactionId, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialTransactionRowFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }

        public BasicFinancialTransactionRow GetFinancialTransactionRow (int financialTransactionRowId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select FinancialTransactionRowId,FinancialAccountId,FinancialTransactionId,AmountCents,CreatedDateTime,CreatedByPersonId FROM FinancialTransactionRows WHERE FinancialTransactionRowId=" +
                        financialTransactionRowId, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadFinancialTransactionRowFromDataReader (reader);
                    }

                    throw new ArgumentException ("Invalid FinancialTransactionRowId");
                }
            }
        }


        public BasicFinancialTransaction[] GetUnbalancedFinancialTransactions (int organizationId)
        {
            List<BasicFinancialTransaction> result = new List<BasicFinancialTransaction>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                string commandString = "select FinancialTransactions.FinancialTransactionId, " +
                                       "FinancialTransactions.OrganizationId, FinancialTransactions.OrganizationSequenceId," +
                                       "FinancialTransactions.DateTime, " +
                                       "FinancialTransactions.Comment, FinancialTransactions.ImportHash, " +
                                       "SUM(FinancialTransactionRows.AmountCents) AS Delta " +
                                       "FROM FinancialTransactions,FinancialTransactionRows " +
                                       "WHERE FinancialTransactionRows.FinancialTransactionId=FinancialTransactions.FinancialTransactionId AND " +
                                       "FinancialTransactions.OrganizationId=" + organizationId + " AND " +
                                       "FinancialTransactionRows.Deleted=0 " +
                                       "GROUP BY FinancialTransactions.FinancialTransactionId HAVING Delta <> 0 " +
                                       "ORDER BY FinancialTransactions.DateTime;";

                DbCommand command = GetDbCommand (commandString, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialTransactionFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }


        public BasicFinancialTransaction GetFinancialTransactionFromDependency (IHasIdentity foreignObject)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                string commandString = "SELECT FinancialTransactionId FROM FinancialTransactionDependencies " +
                                       "JOIN FinancialDependencyTypes ON (FinancialDependencyTypes.FinancialDependencyTypeId=FinancialTransactionDependencies.FinancialDependencyTypeId) " +
                                       "WHERE FinancialDependencyTypes.Name='" + GetForeignTypeString (foreignObject) +
                                       "' AND FinancialTransactionDependencies.ForeignId=" + foreignObject.Identity +
                                       ";";


                DbCommand command = GetDbCommand (commandString, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return GetFinancialTransaction (reader.GetInt32 (0));
                        // Warning: opens another datareader before closing both
                    }

                    throw new ArgumentException (
                        String.Format ("No financial transaction for specified dependency: {0} #{1}",
                            GetForeignTypeString (foreignObject), foreignObject.Identity));
                }
            }
        }


        /// <summary>
        ///     Gets all undocumented transactions that lowers assets for a particular organization
        /// </summary>
        /// <param name="organizationId">The organization owning the transactions</param>
        /// <returns>A list of all such transactions</returns>
        public BasicFinancialTransaction[] GetUndocumentedFinancialTransactions (int organizationId)
        {
            List<BasicFinancialTransaction> result = new List<BasicFinancialTransaction>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                string commandString = "select FinancialTransactions.FinancialTransactionId, " +
                                       "FinancialTransactions.OrganizationId, FinancialTransactions.OrganizationSequenceId, FinancialTransactions.DateTime, " +
                                       "FinancialTransactions.Comment, FinancialTransactions.ImportHash, " +
                                       "SUM(FinancialTransactionRows.AmountCents) AS BalanceDelta " +
                                       "FROM FinancialTransactions " +
                                       "JOIN FinancialTransactionRows ON (FinancialTransactions.FinancialTransactionId=FinancialTransactionRows.FinancialTransactionId) " +
                                       "JOIN FinancialAccounts ON (FinancialAccounts.FinancialAccountId=FinancialTransactionRows.FinancialAccountId) " +
                                       "WHERE FinancialTransactions.OrganizationId=" + organizationId +
                                       " AND FinancialAccounts.AccountType IN (1,2) " +
                                       "AND NOT EXISTS (SELECT DocumentId FROM Documents JOIN DocumentTypes ON (DocumentTypes.DocumentTypeId=Documents.DocumentTypeId) WHERE Documents.ForeignId=FinancialTransactions.FinancialTransactionId AND DocumentTypes.Name='FinancialTransaction') " +
                                       "AND NOT EXISTS (SELECT FinancialTransactionId FROM FinancialTransactionDependencies WHERE FinancialTransactionDependencies.FinancialTransactionId=FinancialTransactions.FinancialTransactionId) " +
                                       "GROUP BY FinancialTransactionId HAVING BalanceDelta < 0;";
                DbCommand command = GetDbCommand (commandString, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialTransactionFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }


        public double GetFinancialAccountsBudget (int[] financialAccountIds, int year)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select SUM(Amount) from FinancialAccountBudgets where Year=" + year +
                        " AND FinancialAccountId IN (" + JoinIds (financialAccountIds) + ");", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read() && !reader.IsDBNull (0))
                    {
                        return reader.GetDouble (0);
                    }

                    return 0.0;
                }
            }
        }

        public double GetFinancialAccountBudget (int financialAccountId, int year)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "select Amount from FinancialAccountBudgets where Year=" + year +
                        " AND FinancialAccountId=" + financialAccountId + ";", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetDouble (0);
                    }

                    return 0.0;
                }
            }
        }

        public Int64[] GetFinancialAccountBudgetMonthly (int financialAccountId, int year)
        {
            List<Int64> result = new List<Int64>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command =
                    GetDbCommand (
                        "select AmountCents from FinancialAccountBudgetsMonthly where Year=" + year +
                        " AND FinancialAccountId=" + financialAccountId + " ORDER BY Month;", connection))

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (reader.GetInt64 (0));
                    }

                    return result.ToArray();
                }
            }
        }

        public void SetFinancialAccountBudget (int financialAccountId, int year, double amount)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountBudget", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "year", year);
                AddParameterWithName (command, "amount", amount);

                command.ExecuteNonQuery();
            }
        }


        public void SetFinancialAccountBudgetMonthly (int financialAccountId, int year, int month, Int64 amountCents)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountBudgetMonthly", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "year", year);
                AddParameterWithName (command, "month", month);
                AddParameterWithName (command, "amountCents", amountCents);

                command.ExecuteNonQuery();
            }
        }


        public void SetFinancialAccountOpenedYear (int financialAccountId, int openedYear)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountOpenedYear", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "openedYear", openedYear);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountClosedYear (int financialAccountId, int closedYear)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountClosedYear", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "closedYear", closedYear);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountParent (int financialAccountId, int parentFinancialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountParent", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "parentFinancialAccountId", parentFinancialAccountId);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountActive (int financialAccountId, bool active)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountActive", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "active", active);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountExpensable (int financialAccountId, bool expensable)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountExpensable", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "expensable", expensable);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountOpen (int financialAccountId, bool open)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountOpen", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "open", open);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountAdministrative (int financialAccountId, bool administrative)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountAdministrative", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "administrative", administrative);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountLinkBackward (int financialAccountId, int linkBackward)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountOwner", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "linkBackward", linkBackward);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountLinkForward (int financialAccountId, int linkForward)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountOwner", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "linkForward", linkForward);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountOwner (int financialAccountId, int ownerPersonId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountOwner", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "ownerPersonId", ownerPersonId);

                command.ExecuteNonQuery();
            }
        }

        public void SetFinancialAccountName (int financialAccountId, string name)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialAccountName", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "name", name);

                command.ExecuteNonQuery();
            }
        }

        private BasicFinancialTransactionRow ReadFinancialTransactionRowFromDataReader (DbDataReader reader)
        {
            int rowId = reader.GetInt32 (0);
            int accountId = reader.GetInt32 (1);
            int transactionId = reader.GetInt32 (2);
            Int64 amountCents = reader.GetInt64 (3);
            DateTime createdDateTime = reader.GetDateTime (4);
            int createdByPersonId = reader.GetInt32 (5);

            return new BasicFinancialTransactionRow (rowId, accountId, transactionId, amountCents, createdDateTime,
                createdByPersonId);
        }

        private BasicFinancialAccount ReadFinancialAccountFromDataReader (DbDataReader reader)
        {
            int accountId = reader.GetInt32 (0);
            string name = reader.GetString (1);
            int organizationId = reader.GetInt32 (2);
            FinancialAccountType accountType = (FinancialAccountType) reader.GetInt32 (3);
            int parentFinancialAccountId = reader.GetInt32 (4);
            int ownerPersonId = reader.GetInt32 (5);
            bool open = reader.GetBoolean (6);
            int openedYear = reader.GetInt32 (7);
            int closedYear = reader.GetInt32 (8);
            bool expensable = reader.GetBoolean (9);
            bool administrative = reader.GetBoolean (10);
            bool active = reader.GetBoolean (11);
            int linkBackward = reader.GetInt32 (12);
            int linkForward = reader.GetInt32 (13);

            return new BasicFinancialAccount (accountId, name, accountType, organizationId, parentFinancialAccountId,
                ownerPersonId, open, openedYear, closedYear, active, expensable, administrative, linkBackward,
                linkForward);
        }

        private BasicFinancialAccountRow ReadFinancialAccountRowFromDataReader (DbDataReader reader)
        {
            int accountId = reader.GetInt32 (0);
            int transactionId = reader.GetInt32 (1);
            int transactionRowId = reader.GetInt32(2);
            DateTime transactionDateTime = reader.GetDateTime (3);
            string comment = reader.GetString (4);
            Int64 amountCents = reader.GetInt64 (5);
            DateTime rowDateTime = reader.GetDateTime (6);
            int rowCreatedByPersonId = reader.GetInt32 (7);

            return new BasicFinancialAccountRow (accountId, transactionId, transactionRowId, transactionDateTime, comment, amountCents,
                rowDateTime, rowCreatedByPersonId);
        }


        private BasicFinancialTransaction ReadFinancialTransactionFromDataReader (DbDataReader reader)
        {
            int transactionId = reader.GetInt32 (0);
            int organizationId = reader.GetInt32 (1);
            int organizationSequenceId = reader.GetInt32(2);
            DateTime dateTime = reader.GetDateTime (3);
            string comment = reader.GetString (4);
            string importHash = reader.GetString (5);

            return new BasicFinancialTransaction (transactionId, organizationId, organizationSequenceId, dateTime, comment, importHash);
        }

        public int CreateFinancialTransaction (int organizationId, DateTime dateTime, string comment)
        {
            if (comment.Length > 127)
            {
                comment = comment.Substring (127); // Dbfield is VARCHAR(128)
            }

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("CreateFinancialTransaction", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "organizationId", organizationId);
                AddParameterWithName (command, "dateTime", dateTime);
                AddParameterWithName (command, "comment", comment);

                return Convert.ToInt32 (command.ExecuteScalar());
            }
        }

        public int CreateFinancialTransactionStub (int organizationId, DateTime dateTime, int financialAccountId,
            Int64 amountCents, string comment, string importHash, int personId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("CreateFinancialTransactionStub", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "dateTime", dateTime);
                AddParameterWithName (command, "organizationId", organizationId);
                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "comment", comment);
                AddParameterWithName (command, "importHash", importHash);
                AddParameterWithName (command, "amountCents", amountCents);
                AddParameterWithName (command, "personId", personId);

                return Convert.ToInt32 (command.ExecuteScalar());
            }
        }


        public int CreateFinancialTransactionRow (int financialTransactionId, int financialAccountId, Int64 amountCents,
            int personId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("CreateFinancialTransactionRow", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialTransactionId", financialTransactionId);
                AddParameterWithName (command, "financialAccountId", financialAccountId);
                AddParameterWithName (command, "amountCents", amountCents);
                AddParameterWithName (command, "dateTime", DateTime.Now);
                AddParameterWithName (command, "personId", personId);

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void SetFinancialTransactionDescription (int financialTransactionId, string description)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialTransactionDescription", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialTransactionId", financialTransactionId);
                AddParameterWithName (command, "description", description);

                command.ExecuteNonQuery();
            }
        }


        public void SetFinancialTransactionDependency (int financialTransactionId,
            FinancialDependencyType dependencyType,
            int foreignId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("SetFinancialTransactionDependency", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialTransactionId", financialTransactionId);
                AddParameterWithName (command, "financialDependencyType", dependencyType.ToString());
                AddParameterWithName (command, "foreignId", foreignId);

                command.ExecuteNonQuery();
            }
        }


        public void ClearFinancialTransactionDependency (int financialTransactionId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("ClearFinancialTransactionDependency", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "financialTransactionId", financialTransactionId);

                command.ExecuteNonQuery();
            }
        }




        public void SetFinancialTransactionOrganizationSequenceId(int financialTransactionId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand("SetFinancialTransactionOrganizationSequenceId", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName(command, "financialTransactionId", financialTransactionId);

                command.ExecuteNonQuery();
            }
        }


        public BasicFinancialTransaction[] GetAllFinancialTransactionsWithoutSequenceNumber()
        {
            List<BasicFinancialTransaction> result = new List<BasicFinancialTransaction>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand(
                        "SELECT " + financialTransactionFieldSequence + " WHERE OrganizationSequenceId=0;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(ReadFinancialTransactionFromDataReader(reader));
                    }

                    return result.ToArray();
                }
            }
        }
    


        public void SetFinancialTransactionForeignId(int financialTransactionId, FinancialForeignIdType foreignIdType, string foreignId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command = GetDbCommand ("SetFinancialTransactionForeignId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    AddParameterWithName (command, "financialTransactionId", financialTransactionId);
                    AddParameterWithName (command, "financialTransactionForeignIdType", (int) foreignIdType);
                    AddParameterWithName (command, "foreignId", foreignId);

                    command.ExecuteNonQuery();
                }
            }
        }



        public int GetFinancialTransactionFromForeignId(FinancialForeignIdType foreignIdType, string foreignId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command =
                    GetDbCommand(
                        "Select FinancialTransactionId from FinancialTransactionForeignIds WHERE FinancialTransactionForeignIdType=" + (int)foreignIdType +
                        " AND ForeignId='" + SqlSanitize(foreignId) + "';", connection))

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }

                    return 0; // none found. Todo: throw exception instead? This behavior is inconsistent
                }
            }
        }



        public Int64 GetFinancialTransactionRowAmountForeignCents(int financialTransactionRowId, int currencyId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command =
                    GetDbCommand(
                        "SELECT NativeAmountCents from FinancialTransactionRowsNativeCurrency WHERE FinancialTransactionRowId=" + financialTransactionRowId +
                        " AND CurrencyId =" + currencyId + ";", connection))

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt64(0);
                    }

                    return 0; // none found. Todo: throw exception instead? This behavior is inconsistent
                }
            }
        }



        public void SetFinancialTransactionRowAmountForeignCents(int financialTransactionRowId, int currencyId, Int64 nativeAmountCents)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command = GetDbCommand("SetFinancialTransactionRowNativeCurrency", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    AddParameterWithName(command, "financialTransactionRowId", financialTransactionRowId);
                    AddParameterWithName(command, "currencyId", currencyId);
                    AddParameterWithName(command, "nativeAmountCents", nativeAmountCents);

                    command.ExecuteNonQuery();
                }
            }
        }



        public Int64 GetFinancialAccountForeignCentsBalance(int financialAccountId)
        {
            // This gets the non-presentation balance across the entire ledger history, so it's only 
            // for asset or liability accounts (not for P&L).

            // This function joins one less table than the ForeignDeltaCents, so it's kept despite being redundant
            // because it's far more optimized for this special case.

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command =
                    GetDbCommand(
                        "SELECT SUM(NativeAmountCents) FROM FinancialTransactionRowsNativeCurrency " +
                        "  JOIN FinancialTransactionRows USING (FinancialTransactionRowId) " +
                        "  WHERE FinancialAccountId=@financialAccountId;", connection))
                {
                    AddParameterWithName(command, "financialAccountId", financialAccountId);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                return reader.GetInt64(0);
                            }
                        }

                        return 0; // zero balance, apparently.
                    }
                }
            }
        }

        public Int64 GetFinancialAccountForeignDeltaCents(int financialAccountId, DateTime lowerBoundInclusive, DateTime upperBoundExclusive)
        {
            // This gets the balance across the specified part of ledger history for non-presentation currency.

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                using (DbCommand command =
                    GetDbCommand(
                        "SELECT SUM(NativeAmountCents) FROM FinancialTransactionRowsNativeCurrency " +
                        "  JOIN FinancialTransactionRows USING (FinancialTransactionRowId) " +
                        "  JOIN FinancialTransactions USING (FinancialTransactionId) " +
                        "  WHERE FinancialTransactionRows.FinancialAccountId=@financialAccountId " +
                        "    AND FinancialTransactions.DateTime >= @lowerBoundInclusive " +
                        "    AND FinancialTransactions.DateTime <  @upperBoundExclusive " +
                        ";", connection))
                {
                    AddParameterWithName(command, "financialAccountId", financialAccountId);
                    AddParameterWithName(command, "lowerBoundInclusive", lowerBoundInclusive);
                    AddParameterWithName(command, "upperBoundExclusive", upperBoundExclusive);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                return reader.GetInt64(0);
                            }
                        }

                        return 0; // zero delta for this time span: no rows returned at all.
                    }
                }
            }
        }





        public DateTime GetFinancialAccountFirstTransactionDate(int financialAccountId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT FinancialTransactions.DateTime FROM FinancialTransactions,FinancialTransactionRows " +
                        "WHERE FinancialTransactions.FinancialTransactionId=FinancialTransactionRows.FinancialTransactionId " +
                        "AND FinancialTransactionRows.FinancialAccountId = " +
                        financialAccountId.ToString (CultureInfo.InvariantCulture) + " " +
                        "ORDER BY FinancialTransactions.DateTime LIMIT 1;", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetDateTime (0);
                    }
                    throw new Exception ("No transactions for this account yet");
                    // need better type here than "Exception"
                    ;
                }
            }
        }

        public BasicFinancialTransaction[] GetDependentFinancialTransactions (FinancialDependencyType dependencyType,
            int foreignId)
        {
            List<int> transactionIds = new List<int>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT FinancialTransactionId FROM FinancialTransactionDependencies,FinancialDependencyTypes " +
                        "WHERE FinancialDependencyTypes.Name='" + dependencyType + "' AND " +
                        "FinancialDependencyTypes.FinancialDependencyTypeId=FinancialTransactionDependencies.FinancialDependencyTypeId AND " +
                        "FinancialTransactionDependencies.ForeignId=" + foreignId, connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        transactionIds.Add (reader.GetInt32 (0));
                    }
                }
            }

            if (transactionIds.Count == 0)
            {
                return new BasicFinancialTransaction[0];
            }

            return GetFinancialTransactions (transactionIds.ToArray());
        }


        // The function below uses OUT parameters, which is a no-go according to .Net Design Guidelines.
        // Fix this by introducing the BasicFinancialDependency type in the semi-near future, and
        // using it as a return type.

        public void GetFinancialTransactionDependency (int financialTransactionId,
            out FinancialDependencyType dependencyType, out int foreignId)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT FinancialDependencyTypes.Name,FinancialTransactionDependencies.ForeignId " +
                        "FROM FinancialTransactionDependencies,FinancialDependencyTypes " +
                        "WHERE FinancialDependencyTypes.FinancialDependencyTypeId=FinancialTransactionDependencies.FinancialDependencyTypeId " +
                        "AND FinancialTransactionDependencies.FinancialTransactionId=" + financialTransactionId,
                        connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Set OUT parameters

                        dependencyType =
                            (FinancialDependencyType)
                                Enum.Parse (typeof (FinancialDependencyType), reader.GetString (0));
                        foreignId = reader.GetInt32 (1);
                    }
                    else
                    {
                        // Set OUT parameters

                        dependencyType = FinancialDependencyType.Unknown;
                        foreignId = 0;
                    }
                }
            }
        }


        // TODO: Return BasicFinancialValidation object
        public void CreateFinancialValidation(FinancialValidationType validationType,
            FinancialDependencyType dependencyType, int foreignId,
            DateTime validatedDateTime, int personId, Int64 amountCents)
        {
            CreateFinancialValidation(validationType, dependencyType,
                foreignId, validatedDateTime, personId, amountCents/100.0);
        }



        public void CreateFinancialValidation (FinancialValidationType validationType,
            FinancialDependencyType dependencyType, int foreignId,
            DateTime validatedDateTime, int personId, double amount)
        {
            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command = GetDbCommand ("CreateFinancialValidation", connection);
                command.CommandType = CommandType.StoredProcedure;

                AddParameterWithName (command, "validationType", validationType.ToString());
                AddParameterWithName (command, "dependencyType", dependencyType.ToString());
                AddParameterWithName (command, "foreignId", foreignId);
                AddParameterWithName (command, "validatedDateTime", validatedDateTime);
                AddParameterWithName (command, "personId", personId);
                AddParameterWithName (command, "amount", amount);

                command.ExecuteNonQuery();
            }
        }


        // -- lines and trees and children --


        public BasicFinancialAccount[] GetFinancialAccountChildren (int parentFinancialAccountId)
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            using (DbConnection connection = GetMySqlDbConnection())
            {
                connection.Open();

                DbCommand command =
                    GetDbCommand (
                        "SELECT " + financialAccountFieldSequence + " WHERE ParentFinancialAccountId = " +
                        parentFinancialAccountId +
                        " ORDER BY \"Name\"", connection);

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add (ReadFinancialAccountFromDataReader (reader));
                    }

                    return result.ToArray();
                }
            }
        }


        public Dictionary<int, List<BasicFinancialAccount>> GetHashedFinancialAccounts (int organizationId)
        {
            // This generates a Dictionary <int,List<Node>>.
            // 
            // Keys are integers corresponding to NodeIds. At each key n,
            // the value is an List<Node> starting with the node n followed by
            // its children.
            //
            // (Later reflection:) O(n) complexity, instead of recursion. Nice!

            Dictionary<int, List<BasicFinancialAccount>> result = new Dictionary<int, List<BasicFinancialAccount>>();

            BasicFinancialAccount[] nodes = GetFinancialAccountsForOrganization (organizationId);

            // Add the root.

            result[0] = new List<BasicFinancialAccount>();

            // Add the nodes.

            foreach (BasicFinancialAccount node in nodes)
            {
                List<BasicFinancialAccount> newList = new List<BasicFinancialAccount>();
                newList.Add (node);

                result[node.FinancialAccountId] = newList;
            }

            // Add the children.

            foreach (BasicFinancialAccount node in nodes)
            {
                result[node.ParentFinancialAccountId].Add (node);
            }

            return result;
        }


        public BasicFinancialAccount[] GetFinancialAccountLine (int leafFinancialAccountId)
        {
            int orgId = GetFinancialAccount (leafFinancialAccountId).OrganizationId;

            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            Dictionary<int, List<BasicFinancialAccount>> nodes = GetHashedFinancialAccounts (orgId);

            BasicFinancialAccount currentNode = nodes[leafFinancialAccountId][0];

            // This iterates until the zero-parentid root node is found

            while (currentNode != null && currentNode.ParentFinancialAccountId != 0)
            {
                result.Add (currentNode);

                if (currentNode.ParentFinancialAccountId != 0)
                {
                    currentNode = nodes[currentNode.ParentFinancialAccountId][0];
                }
                else
                {
                    currentNode = null;
                }
            }

            result.Reverse();

            return result.ToArray();
        }


        public BasicFinancialAccount[] GetFinancialAccountTreeForOrganization (int organizationId)
        {
            Dictionary<int, List<BasicFinancialAccount>> nodes = GetHashedFinancialAccounts (organizationId);

            return GetFinancialAccountTree (nodes, 0, 0);
        }

        /*
        public Dictionary<int, BasicFinancialAccount> GetFinancialAccountHashtable(int startFinancialAccountId)
        {
            BasicFinancialAccount[] nodes = GetFinancialAccountTree(startFinancialAccountId);

            Dictionary<int, BasicFinancialAccount> result = new Dictionary<int, BasicFinancialAccount>();

            foreach (BasicFinancialAccount node in nodes)
            {
                result[node.FinancialAccountId] = node;
            }

            return result;
        }*/


        private BasicFinancialAccount[] GetFinancialAccountTree (
            Dictionary<int, List<BasicFinancialAccount>> financialAccounts, int startNodeId,
            int generation)
        {
            List<BasicFinancialAccount> result = new List<BasicFinancialAccount>();

            List<BasicFinancialAccount> thisList = financialAccounts[startNodeId];

            foreach (BasicFinancialAccount node in thisList)
            {
                if (node.FinancialAccountId != startNodeId)
                {
                    result.Add (new BasicFinancialAccount (node));

                    // Add recursively

                    BasicFinancialAccount[] children = GetFinancialAccountTree (financialAccounts,
                        node.FinancialAccountId, generation + 1);

                    if (children.Length > 0)
                    {
                        foreach (BasicFinancialAccount child in children)
                        {
                            result.Add (child);
                        }
                    }
                }
                else if (generation == 0 && startNodeId != 0)
                {
                    // The top parent is special and should be added (unless null); the others shouldn't

                    result.Add (new BasicFinancialAccount (node));
                }
            }

            return result.ToArray();
        }
    }
}