﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace TellMeYourSecrets
{
    class CheckPrivileges : Base
    {            
        public Boolean croak = false;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetSystem()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            if (!currentIdentity.IsSystem)
            {
                WindowsPrincipal currentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

                WriteOutputNeutral("Not running as SYSTEM, checking for Administrator access.");
                WriteOutputNeutral(String.Format("Operating as {0}", WindowsIdentity.GetCurrent().Name));

                if (CheckAdministrator(currentIdentity))
                {
                    WriteOutputNeutral("Attempting to elevate to SYSTEM");
                    new Tokens().GetSystem();
                    if (!WindowsIdentity.GetCurrent().IsSystem)
                    {
                        WriteOutputBad("GetSystem Failed");
                        croak = true;
                        return false;
                    }
                    WriteOutputGood("Running as SYSTEM");
                    WriteOutput(" ");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                WriteOutputGood("Running as SYSTEM");
                return true;
            }
            
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://blogs.msdn.microsoft.com/cjacks/2006/10/08/how-to-determine-if-a-user-is-a-member-of-the-administrators-group-with-uac-enabled-on-windows-vista/
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean CheckAdministrator(WindowsIdentity currentIdentity)
        {
            if ((new WindowsPrincipal(currentIdentity)).IsInRole(WindowsBuiltInRole.Administrator))
            {
                WriteOutputGood("Running as Administrator");
                return true;
            }

            IntPtr hToken = currentIdentity.Token;
            UInt32 tokenInformationLength = (UInt32)Marshal.SizeOf(typeof(UInt32));
            IntPtr tokenInformation = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)));
            UInt32 returnLength;

            Boolean result = advapi32.GetTokenInformation(
                hToken,
                Enums._TOKEN_INFORMATION_CLASS.TokenElevationType,
                tokenInformation,
                tokenInformationLength,
                out returnLength
            );

            switch ((Enums.TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(tokenInformation))
            {
                case Enums.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault:
                    WriteOutputBad("TokenElevationTypeDefault");
                    WriteOutputNeutral("Token: Not Split");
                    WriteOutputNeutral("ProcessIntegrity: Medium/Low");
                    return false;
                case Enums.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull:
                    WriteOutputGood("TokenElevationTypeFull");
                    WriteOutputNeutral("Token: Split");
                    WriteOutputNeutral("ProcessIntegrity: High");
                    return true;
                case Enums.TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited:
                    WriteOutputNeutral("TokenElevationTypeLimited");
                    WriteOutputNeutral("Token: Split");
                    WriteOutputNeutral("ProcessIntegrity: Medium/Low");
                    WriteOutputNeutral("Hint: Run as Administrator or Bypass UAC");
                    return false;
                default:
                    WriteOutputBad("Unknown integrity");
                    WriteOutputNeutral("Trying anyway");
                    return true;
            }
        }
    }
}