using System;

namespace Client.DAO.Models
{
    public class MasterRecord : IEquatable<MasterRecord>
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public bool Equals(MasterRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(MasterRecord left, MasterRecord right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MasterRecord left, MasterRecord right)
        {
            return !Equals(left, right);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MasterRecord) obj);
        }
    }
}