using System;
using Heleus.Base;

namespace Heleus.Service
{
    public enum ServiceResultTypes
    {
        Ok = 0,
        False = 1,
        PurchaseRequired = 2
    }

    public struct ServiceResult
    {
        public readonly ServiceResultTypes Result;
        public readonly long UserCode;
        public readonly string Message;

        public bool IsOK => Result == ServiceResultTypes.Ok;

        public static ServiceResult Ok = new ServiceResult(ServiceResultTypes.Ok);

        public ServiceResult(ServiceResultTypes result)
        {
            Result = result;
            UserCode = 0;
            Message = null;
        }

        public ServiceResult(ServiceResultTypes result, string message)
        {
            Result = result;
            UserCode = 0;
            Message = message;
        }

        public ServiceResult(ServiceResultTypes result, long userCode)
        {
            Result = result;
            UserCode = userCode;
            Message = null;
        }

        public ServiceResult(ServiceResultTypes result, long userCode, string message)
        {
            Result = result;
            UserCode = userCode;
            Message = message;
        }

        public ServiceResult(Unpacker unpacker)
        {
            Result = (ServiceResultTypes)unpacker.UnpackShort();
            unpacker.Unpack(out UserCode);
            unpacker.Unpack(out Message);
        }

        public void Pack(Packer packer)
        {
            packer.Pack((short)Result);
            packer.Pack(UserCode);
            packer.Pack(Message);
        }
    }
}
