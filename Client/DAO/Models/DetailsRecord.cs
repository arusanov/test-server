using System;

namespace Client.DAO.Models
{
    public class DetailsRecord : IEquatable<DetailsRecord>
    {
        public long MasterRecordId { get; set; }

        public string Name { get; set; }

        public bool Equals(DetailsRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MasterRecordId == other.MasterRecordId && string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MasterRecordId.GetHashCode()*397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DetailsRecord left, DetailsRecord right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DetailsRecord left, DetailsRecord right)
        {
            return !Equals(left, right);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DetailsRecord) obj);
        }
    }
}