﻿using System;

namespace Boxofon.Web.Membership
{
    public interface IUserRepository
    {
        User GetById(Guid id);
        Guid? GetIdByProviderNameAndProviderUserId(string providerName, string providerUserId);
        User GetByTwilioAccountSid(string twilioAccountSid);
        void Save(User user);
    }
}