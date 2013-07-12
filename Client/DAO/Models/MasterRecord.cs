using System.ComponentModel.DataAnnotations;

namespace Client.DAO.Models
{
    public class MasterRecord
    {
        protected bool Equals(MasterRecord other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MasterRecord) obj);
        }
    }
}