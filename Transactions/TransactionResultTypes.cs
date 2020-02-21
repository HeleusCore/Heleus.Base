using System;
namespace Heleus.Transactions
{
    public enum TransactionResultTypes
    {
        Ok = 0,
        Unknown = 1,

        InvalidTransaction = 10,
        InvalidContent = 11,
        Expired = 12,
        InvalidCoreAccount = 13,
        InvalidServiceAccount = 14,
        InvalidServiceAccountKey = 15,
        AlreadySubmitted = 16,
        AlreadyProcessed = 17,
        InvalidSignature = 18,

        InvalidReceiverAccount = 54,
        InvalidTransferReason = 55,
        InsuficientBalance = 56,

        ChainKeyRevoked = 100,
        ChainKeyExpired = 101,
        ChainNotFound = 102,
        ChainKeyNotFound = 103,
        ChainNodeInvalid = 104,
        ChainServiceUnavailable = 105,
        ChainServiceErrorResponse = 106,

        InvalidChainName = 150,
        InvalidChainWebsite = 151,
        InvalidChainEndpoint = 152,
        InvalidChainPurchase = 153,
        InvaidChainKey = 154,

        InvalidBlock = 200,
        BlockLimitExceeded = 201,
        InvalidBlockSignature = 202,

        AlreadyJoined = 500,

        CannotPurchase = 520,
        PurchaseNotFound = 521,
        PurchaseRequired = 522,

        AttachementsNotUploaded = 530,
        AttachementsNotAllowed = 531,
        AttachementsInvalid = 532,
        AttachementsUploadFailed = 533,

        FeatureUnknown = 1000,
        FeatureNotAvailable = 1001,
        FeatureMissing = 1002,
        FeatureInternalError = 1003,
        FeatureCustomError = 1004,

        RevenueAmoutInvalid = 2000,
    }
}
