using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BankService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Banking" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Banking.svc or Banking.svc.cs at the Solution Explorer and start debugging.
    public class Banking : IBanking
    {
        BankDataModelDataContext dt = new BankDataModelDataContext();
        public bool AddAccount(Account account)
        {
            try
            {
                string salt = Guid.NewGuid().ToString().Substring(0, 7);
                account.salt = salt;
                string pin = "123456";
                var str = pin + salt;
                var MD5Pass = Encryptor.MD5Hash(str);
                account.pin = MD5Pass;
                account.balance = 50000;
                account.createdAt = DateTime.Now;
                account.updatedAt = DateTime.Now;
                account.status = 0;
                dt.Accounts.InsertOnSubmit(account);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddCustomer(Customer cus)
        {
            try
            {
                if (dt.Customers.Any(y => y.id == cus.id))
                {
                    return false;
                }

                var id = cus.id;
                Account acount = (from ac in dt.Accounts
                                  where ac.accountNumber == id
                                  && ac.status == 0
                                  select ac).Single();
                if (acount == null)
                {
                    return false;
                }
                cus.createAt = DateTime.Now;
                cus.updateAt = DateTime.Now;
                acount.status = 1;

                dt.Customers.InsertOnSubmit(cus);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddTransaction(Transaction transaction)
        {
            try
            {
                var payid = 2;

                Account senderAC = (from ac in dt.Accounts
                                    where ac.accountNumber == transaction.senderAccountNumber
                                    && ac.status == 1
                                    select ac).Single();

                Account receiverAccount = (from ac in dt.Accounts
                                           where ac.accountNumber == transaction.receiverAccountNumber
                                           && ac.status == 1
                                           select ac).Single();

                Account paypal = (from ac in dt.Accounts
                                  where ac.accountNumber == payid
                                  && ac.status == 1
                                  select ac).Single();

                var queryBalanceSender = (from b in dt.Accounts
                                          where b.accountNumber == transaction.senderAccountNumber
                                          && b.status == 1
                                          select b.Balance).FirstOrDefault().ToString();
                if (queryBalanceSender == null)
                {
                    return false;
                }

                var queryBalanceReceiver = (from b in dt.Accounts
                                            where b.accountNumber == transaction.receiverAccountNumber
                                            && b.status == 1
                                            select b.Balance).FirstOrDefault().ToString();

                if (queryBalanceReceiver == null)
                {
                    return false;
                }

                var queryBalancePaypal = (from b in dt.Accounts
                                          where b.accountNumber == payid
                                          && b.status == 1
                                          select b.Balance).FirstOrDefault().ToString();

                if (queryBalancePaypal == null)
                {
                    return false;
                }

                decimal fee = 0;
                var amount = Convert.ToInt64(transaction.amount);
                if (amount <= 100000)
                {
                    fee = 10000;
                }
                else if (100000 < amount && amount <= 500000)
                {
                    long i = (2 * amount) / 100;
                    fee = i;
                }
                else if (500000 < amount && amount <= 1000000)
                {
                    long i = Convert.ToInt64(1.5 * amount) / 100;
                    fee = i;
                }
                else if (1000000 < amount && amount <= 5000000)
                {
                    long i = (1 * amount) / 100;
                    fee = i;
                }
                else if (amount > 5000000)
                {
                    long i = Convert.ToInt64(0.5 * amount) / 100;
                }

                if ((amount + fee) > Convert.ToInt64(queryBalanceSender))
                {
                    return false;
                }

                senderAC.balance = Convert.ToInt64(queryBalanceSender) - amount - fee;
                senderAC.updatedAt = DateTime.Now;

                receiverAccount.balance = Convert.ToInt64(queryBalanceReceiver) + amount;
                receiverAccount.updatedAt = DateTime.Now;

                paypal.balance = Convert.ToInt64(queryBalancePaypal) + fee;
                paypal.updatedAt = DateTime.Now;

                transaction.feeTransaction = fee;
                dt.Transactions.InsertOnSubmit(transaction);
                dt.SubmitChanges();
                return true;

            }
            catch
            {
                return false;
            }

        }

        public Account GetAccountById(long id)
        {
            throw new NotImplementedException();
        }

        public bool LoginAccount(Account account)
        {
            try
            {
                string pass = account.pin.ToString();

                var salt = (from c in dt.Accounts
                            where c.accountNumber == account.accountNumber
                            select c.salt).FirstOrDefault().ToString();

                if (salt == null)
                {
                    return false;
                }
                var str = pass + salt;
                var MD5Pass = Encryptor.MD5Hash(str);

                var acount = dt.Accounts.Where(x => x.accountNumber == account.accountNumber && x.pin == MD5Pass).FirstOrDefault();
                if (acount == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
