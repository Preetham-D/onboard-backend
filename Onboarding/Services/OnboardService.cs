﻿using Chilkat;
using Consul;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using Onboarding.Contract;
using Onboarding.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Onboarding.Services
{
    public class OnboardService : IOnboardingService
    {
        private readonly OnboardingContext _context;

        public OnboardService(OnboardingContext context)
        {
            _context = context;
        }


        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        public string SendMail(LoginViewModel value)
        {
            //generate token 
            string token = RandomString(6);

            //instantiate mimemsg
            var message = new MimeMessage();

            //from add
            message.From.Add(new MailboxAddress("TL;DM", "talklessDM@gmail.com"));
            //to add
            message.To.Add(new MailboxAddress("Hi", value.EmailId));
            //subject
            message.Subject = "Verification Mail";

            //body

            if (value.Workspace == null)
            {
                message.Body = new TextPart("plain")
                {
                    Text = "Welcome to TL;DM your temporaray token is  " + token + " Welcome Aboard!"
                };

            }
            else
            {
                message.Body = new TextPart("plain")
                {
                    Text = "Welcome to TL;DM You have been invited to join " + value.Workspace + " Your Temporary token is " + token + " Welcome Aboard!"
                };
            }

            //Configure and send email

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("talklessDM@gmail.com", "tldm1234");
                client.Send(message);
                client.Disconnect(true);

            }
            return token;
        }

        public async Task<Object> CreateWorkspace(Workspace workspace)
        {
            //var  unique =  _context.UserAccount.Include(i => i.Workspaces).Where(x => x.Workspaces.TrueForAll(y => y.WorkspaceName == workspace.WorkspaceName));

            var unique = await _context.Workspace.FirstOrDefaultAsync(x => x.WorkspaceName == workspace.WorkspaceName);

            // var unique = await _context.Workspace.FirstOrDefaultAsync(i => workspace.Workspaces.Any(y => y.WorkspaceName == i.WorkspaceName));
            if (unique == null)
            {
                _context.Workspace.Add(workspace);
                // UserAccount user =  new UserAccount { new List<Workspace>() { new Workspace { WorkspaceName = workspace.WorkspaceName } } };
                _context.SaveChanges();
                return workspace;
            }
            return null;
        }

        public async Task<Object> OnboardUser(LoginViewModel value)
        {

            var workspace = await _context.Workspace.FirstOrDefaultAsync(x => x.WorkspaceName == value.Workspace);

            // var user = await _context.UserAccount.FirstAsync(x => x.Workspaces.FirstOrDefault(u => u.WorkspaceName == value.EmailId))

            if (workspace != null)
            {
                string token = SendMail(value);

                UserState user = new UserState() { EmailId = value.EmailId, Otp = token };
                //_context.Workspace.Where(x => x.WorkspaceName == value.Workspace).Include(x => x.UsersState);
                workspace.UsersState.Add(user);

                var newuser = await _context.UserAccount.Include(i => i.Workspaces).FirstOrDefaultAsync(x => x.EmailId == value.EmailId);

                if (newuser == null)
                {
                    UserAccount details = new UserAccount() { EmailId = value.EmailId };
                    await _context.UserWorkspaces.AddAsync(new UserWorkspace { Workspace = workspace, UserAccount = details });
                    _context.SaveChanges();
                }
                //else
                //{
                //    newuser.Workspaces.AddRange(user.Workspaces);
                //    _context.UserAccount.Update(newuser);
                //    _context.SaveChanges();
                //}

                _context.Workspace.Update(workspace);
                // _context.UserState.Add(user);
                _context.SaveChanges();
                return user;
            }
            return null;
        }

        public async Task<Object> VerifyUser(string token)
        {
            //var user = await _context.UserAccount.FirstOrDefaultAsync(x => x.Password == token);
            //var space = await _context.Workspace.Include(i => i.UsersState).FirstOrDefaultAsync(x => x.WorkspaceName == token.Workspace);
            var user = await _context.UserState.FirstOrDefaultAsync(x => x.Otp == token);
            //var user = space.UsersState.FirstOrDefault(x => x.Otp == token.Password);
            //user.IsVerified = true;
            //user.IsJoined = true;
            //_context.SaveChanges();
            if (user != null)
            {
                var claims = new[]
                   {
                       new Claim(JwtRegisteredClaimNames.Email,user.EmailId),
                   };
                var privateKey = File.ReadAllText(@"E:\workspace\Project\onboard-backend-b8d13474c651ab70c525d5bd4fef72308551a2b4/jwtRS256.key");
                var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey));
                var Jwtoken = new JwtSecurityToken(
                    issuer: "http://oec.com",
                    audience: "http://oec.com",
                    expires: DateTime.UtcNow.AddHours(1),
                    claims: claims,
                    signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256));

                user.IsJoined = true;
                _context.SaveChanges();

                return new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(Jwtoken),
                    expiration = Jwtoken.ValidTo
                };
            }

            return user;
        }


        public async Task<Object> VerifyInvitedUser(LoginViewModel token)
        {
            //var user = await _context.UserAccount.FirstOrDefaultAsync(x => x.Password == token);
            var space = await _context.Workspace.Include(i => i.UsersState).FirstOrDefaultAsync(x => x.WorkspaceName == token.Workspace);
            //var user = await _context.UserState.FirstOrDefaultAsync(x => x.Otp == token);
            var user = space.UsersState.FirstOrDefault(x => x.Otp == token.Password);
            //user.IsVerified = true;
            //user.IsJoined = true;
            //_context.SaveChanges();
            if (user != null)
            {
                var claims = new[]
                   {
                       new Claim(JwtRegisteredClaimNames.Email,user.EmailId),
                   };

                var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecureKey"));
                var Jwtoken = new JwtSecurityToken(
                    issuer: "http://oec.com",
                    audience: "http://oec.com",
                    expires: DateTime.UtcNow.AddHours(1),
                    claims: claims,
                    signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256));

                user.IsJoined = true;
                _context.SaveChanges();

                return new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(Jwtoken),
                    expiration = Jwtoken.ValidTo
                };
            }

            return user;
        }

        public async Task<UserAccount> PersonalDetails(UserAccount user)
        {
            var newuser = await _context.UserAccount.Include(i => i.Workspaces).FirstOrDefaultAsync(x => x.EmailId == user.EmailId);

            if (newuser != null)
            {
                newuser.FirstName = user.FirstName;
                newuser.LastName = user.LastName;
                newuser.Password = user.Password;
                newuser.IsVerified = true;

                var workspaceName = user.Workspaces.Select(i => i.Name).ToList();
                // workspaceName.BinarySearch
                //newuser.Workspaces.BinarySearch(workspaceName.)
                var v = newuser.Workspaces.Exists(x => x.Name == workspaceName[0]);
                if (!v)
                {
                    newuser.Workspaces.AddRange(user.Workspaces);
                    //newuser.Workspaces.ToHashSet();
                }

                _context.UserAccount.Update(newuser);
                _context.SaveChanges();
            }
            // else
            //{
            //newuser.Workspaces.AddRange(user.Workspaces);
            //_context.UserAccount.Update(newuser);
            //_context.SaveChanges();
            //}
            return newuser;
        }

        public async Task<Workspace> WorkSpaceDetails(Workspace workspace)
        {
            var space = await _context.Workspace.FirstOrDefaultAsync(x => x.WorkspaceName == workspace.WorkspaceName);
            // workspace.Id = space.Id;
            space.Channels.AddRange(workspace.Channels);
            space.Bots.AddRange(workspace.Bots);
            space.PictureUrl = workspace.PictureUrl;
            // space.UsersState.AddRange(workspace.UsersState);

            _context.Workspace.Update(space);
            _context.SaveChanges();
            return space;
        }

        public async Task<Object> OnboardUserFromWorkspace(LoginViewModel value)
        {
            //var user = await _context.Workspace.Include(x => x.UsersState).FirstOrDefaultAsync(v => v.UsersState.Exists(o => o.EmailId == value.EmailId));
            var workspace = await _context.Workspace.Include(i => i.UsersState).FirstOrDefaultAsync(x => x.WorkspaceName == value.Workspace);
            var user = workspace.UsersState.FirstOrDefault(x => x.EmailId == value.EmailId);

            if (user == null || !user.IsJoined)
            {
                var otp = SendMail(value);
                var newuser = await _context.UserAccount.Include(i => i.Workspaces).FirstOrDefaultAsync(x => x.EmailId == value.EmailId);

                if (newuser == null)
                {
                    var details = new UserAccount() { EmailId = value.EmailId };
                    await _context.UserWorkspaces.AddAsync(new UserWorkspace { Workspace = workspace, UserAccount = details });
                    await _context.UserAccount.AddAsync(details);
                    _context.SaveChanges();
                    // return details;
                }
                UserState newUser = new UserState() { EmailId = value.EmailId, Otp = otp };
                workspace.UsersState.Add(newUser);
                _context.Workspace.Update(workspace);
                _context.SaveChanges();
                return newUser;

            }

            //else
            //{
            //    newuser.Workspaces.AddRange(user.Workspaces);
            //    _context.UserAccount.Update(newuser);
            //    _context.SaveChanges();
            //}
            //UserState newUser = new UserState() { EmailId = value.EmailId, Otp = otp };
            //workspace.UsersState.Add(newUser);
            //_context.Workspace.Update(workspace);
            //_context.SaveChanges();
            return null;

        }

        public async Task<IEnumerable> GetAllWorkspace(string value)
        {
            // var list = _context.UserAccount.Include(x => x.Workspaces).Where(c => c.Workspaces.Any(u => u.Name == u.Name));
            var user = await _context.UserAccount.Include(t => t.Workspaces).FirstOrDefaultAsync(x => x.EmailId == value);
            var list = user.Workspaces.Select(v => v.Name);

            return list;
        }

        public async Task<JsonObject> Login(LoginViewModel login)
        {
            var user = await _context.UserAccount.Where(existUser =>
           existUser.EmailId == login.EmailId
           && existUser.Password == login.Password)
           .FirstOrDefaultAsync();

            if (user != null)
            {
                
                // chilkat       
                    // JsonObject jwtHeader = new JsonObject();
                    // jwtHeader.AppendString("alg", "RS256");
                    // jwtHeader.AppendString("typ", "JWT");

                    JsonObject claims = new JsonObject();
                    claims.AppendString("Email", user.EmailId);
                    claims.AppendString("UserID", user.Id);

                    return claims;

                    //Object required = new object()
                    //{
                    //    Header = jwtHeader,

                    //}

                   // Jwt jwt = new Jwt();

                 //   string token = jwt.CreateJwtPk(jwtHeader.Emit(), claims.Emit(), privateKey);

                    //return token;
                }

            return null;
        }

        //public static string CreateToken(List<Claim> claims, string privateRsaKey)
        //{
        //    RSAParameters rsaParams;
        //    using (var tr = new StringReader(privateRsaKey))
        //    {
        //        var pemReader = new PemReader(tr);
        //        var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
        //        if (keyPair == null)
        //        {
        //            throw new Exception("Could not read RSA private key");
        //        }
        //        Console.WriteLine(keyPair.Private.ToString());
        //        var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
        //        rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
        //    }
        //    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        //    {
        //        rsa.ImportParameters(rsaParams);
        //        Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
        //        return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
        //    }
        //}

        public async Task<Workspace> GetWorkspaceByName(string name)
        {
            var space = await _context.Workspace.Include(x => x.UsersState).Include(y => y.Channels)
                .Include(z => z.UserWorkspaces).FirstOrDefaultAsync(i => i.WorkspaceName == name);
            //var user = await _context.UserAccount.FirstOrDefaultAsync(i => i.EmailId == name);
            return space;
        }

    }
}
