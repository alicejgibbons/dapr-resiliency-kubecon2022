using System;
using System.Text.Json.Serialization;

namespace CustomerLoyaltyJob.Model
{
    public class BlobItem
    {
        public object XMLName { get; set; }

        public string Name { get; set; }

        public bool Deleted { get; set; }
        
        public string Snapshot { get; set; }

        public object Properties { get; set; }

        public string Metadata { get; set; }
        
        // public string CreationTime { get; set; }

        // public string LastModified { get; set; }
        
        // public string retentionExpirationTime { get; set; }

        // public string Etag { get; set; }
        
        // public int ContentLength { get; set; }

        // public string ContentType { get; set; }

        // public string ContentEncoding { get; set; }

        // public string ContentLanguage { get; set; }
        
        // public string ContentMD5 { get; set; }
        
        // public string ContentDisposition { get; set; }

        // public string CacheControl { get; set; }

        // public string BlobSequenceNumber { get; set; }
        
        // public string BlobType{ get; set; }

        // public string LeaseStatus { get; set; }
        
        // public string LeaseState { get; set; }

        // public string LeaseDuration { get; set; }
        
        // public string CopyID { get; set; }

        // public string CopyStatus { get; set; }
        
        // public string CopySource { get; set; }

        // public string CopyProgress { get; set; }
        
        // public string CopyCompletionTime { get; set; }

        // public string CopyStatusDescription { get; set; }
        
        // public string ServerEncrypted { get; set; }

        // public string IncrementalCopy { get; set; }
        
        // public string DestinationSnapshot { get; set; }

        // public string DeletedTime { get; set; }
        
        // public string RemainingRetentionDays { get; set; }

        // public string AccessTier { get; set; }
        
        // public bool AccessTierInferred { get; set; }

        // public string ArchiveStatus { get; set; }
        
        // public string CustomerProvidedKeySha256 { get; set; }

        // public string AccessTierChangeTime { get; set; }

    }
}
