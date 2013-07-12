using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.DAO.Models
{
    public class DetailsRecord
    {
        protected bool Equals(DetailsRecord other)
        {
            return MasterRecordId == other.MasterRecordId && string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MasterRecordId*397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        [Key,Column(Order = 1)]
        public int MasterRecordId { get; set; }

        [ForeignKey("MasterRecordId")]
        public virtual MasterRecord MasterRecord { get; set; }

        [Required,StringLength(400),Key,Column(Order = 2)]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DetailsRecord) obj);
        }
    }
}