using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Configuration.Provider;

namespace MvcForum.Models
{
    public class ForumMembershipProvider : MembershipProvider
    {

        string _Name = "";

        public override string Name
        {
            get
            {
                return _Name;
            }
        }

        public override string ApplicationName
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            _Name = String.IsNullOrEmpty(name) ? "ForumMembershipProvider" : name;
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User UpdateUser = db.GetUser(username);
                if (UpdateUser == null)
                {
                    return false;
                }
                if (!ValidateUser(username, oldPassword)) return false;

                UpdateUser.PasswordHash = CalculateSaltedPasswordHash(newPassword, UpdateUser.Salt);
                db.Save();
                return true;
            }
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotSupportedException();
        }

        private MembershipUser GetMembershipUserFromForumUser(Forum_User ForumUser)
        {
            if (ForumUser == null) return null;
            return new MembershipUser(_Name, ForumUser.Username, ForumUser.UserID, ForumUser.Email, "", "", ForumUser.Approved, false, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            Forum_User NewUser = new Forum_User();
            NewUser.Email = email.Trim();
            NewUser.Username = username.Trim();
            NewUser.Salt = GenerateSalt();
            NewUser.PasswordHash = CalculateSaltedPasswordHash(password, NewUser.Salt);
            NewUser.Approved = isApproved;

            if (String.IsNullOrEmpty(email))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            }
            if (String.IsNullOrEmpty(username))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return null;
            }
            if (String.IsNullOrEmpty(password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            using (var db = new ForumRespository())
            {
                if (db.UserExists(username))
                {
                    status = MembershipCreateStatus.DuplicateUserName;
                    return null;
                }
                db.AddUser(NewUser);
                try
                {
                    db.Save();
                }
                catch
                {
                    status = MembershipCreateStatus.ProviderError;
                    return null;
                }
            }
            status = MembershipCreateStatus.Success;
            return GetMembershipUserFromForumUser(NewUser);
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override bool EnablePasswordReset
        {
            get { return true; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            using (ForumRespository db = new ForumRespository())
            {
                var Users = db.FindUsersByEmail(emailToMatch);
                totalRecords = Users.Count();
                var PageUsers = Users.Skip(pageIndex * pageSize).Take(pageSize);
                var Collection = new MembershipUserCollection();
                foreach (Forum_User user in PageUsers)
                {
                    Collection.Add(GetMembershipUserFromForumUser(user));
                }
                return Collection;
            }
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            using (ForumRespository db = new ForumRespository())
            {
                var Users = db.FindUsersByName(usernameToMatch);
                totalRecords = Users.Count();
                var PageUsers = Users.Skip(pageIndex * pageSize).Take(pageSize);
                var Collection = new MembershipUserCollection();
                foreach (Forum_User user in PageUsers)
                {
                    Collection.Add(GetMembershipUserFromForumUser(user));
                }
                return Collection;
            }
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            using (ForumRespository db = new ForumRespository())
            {
                MembershipUserCollection Collection = new MembershipUserCollection();

                IQueryable<Forum_User> Users = db.GetAllUsers();

                foreach (Forum_User User in Users.Skip(pageIndex * pageSize).Take(pageSize))
                {
                    Collection.Add(GetMembershipUserFromForumUser(User));
                }

                totalRecords = Users.Count();

                return Collection;
            }
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotSupportedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            using (ForumRespository db = new ForumRespository())
            {
                return GetMembershipUserFromForumUser(db.GetUser(username));
            }
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (!(providerUserKey is int)) return null;
            using (ForumRespository db = new ForumRespository())
            {
                return GetMembershipUserFromForumUser(db.GetUserByID((int)providerUserKey));
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotSupportedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotSupportedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotSupportedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotSupportedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotSupportedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotSupportedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return false; }
        }



        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User UpdateUser = db.GetUser(user.UserName);
                if (UpdateUser == null)
                {
                    throw new Exception("User not found in database");
                }
                UpdateUser.Username = user.UserName;
                UpdateUser.Email = user.Email;
                UpdateUser.Approved = user.IsApproved;
                db.Save();
            }
        }

        public override bool ValidateUser(string username, string password)
        {
            string test = GenerateSalt();
            int t = test.Length;
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)) throw new HttpException();
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User ValidateUser = db.GetUser(username);
                if (ValidateUser == null) return false;
                if (!ValidateUser.Approved) return false;
                return (ValidateUser.PasswordHash == CalculateSaltedPasswordHash(password, ValidateUser.Salt));
            }
        }

        public string CalculateSaltedPasswordHash(string Password, string Salt)
        {
            var Hash = new SHA256CryptoServiceProvider();
            Byte[] HashBytes = Encoding.UTF8.GetBytes(Password + Salt);
            Byte[] HashedBytes = Hash.ComputeHash(HashBytes);
            return Convert.ToBase64String(HashedBytes);
        }

        string GenerateSalt()
        {
            var RNG = new RNGCryptoServiceProvider();
            Byte[] Salt = new Byte[8];
            RNG.GetBytes(Salt);
            return Convert.ToBase64String(Salt);
        }

    }
}