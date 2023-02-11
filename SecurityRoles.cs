using System;

namespace Druware.Server
{
    public static class UserSecurityRole
    {
        public const string Unconfirmed = "8471156C-132F-41BE-BD21-D5EB20953DA2";
        public const string Confirmed = "99ED04E0-8BBC-491C-9B8A-9E287BC736F3";
        public const string Manager = "42EECE03-4648-4FF5-9A6B-A39784B7B13A";
        public const string SystemAdministrator = "0CF5915A-E9A2-49F8-B942-AD1D7815F4B7";
        public const string ManagerOrSystemAdministrator = Manager + ", " + SystemAdministrator;
    }
}

