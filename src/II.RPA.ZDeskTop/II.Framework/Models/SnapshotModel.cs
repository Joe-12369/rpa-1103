using System.Drawing;

namespace II.Framework.Models
{
    public  struct SnapshotModel
    {
        public SnapshotModel(Bitmap bitmap, 
            long number)
        {
            Picture = bitmap;
            Number = number;
        }
        public Bitmap Picture 
        { 
            get;
            private set; 
        }
        public long Number
        {
            get;
            private set;
        }
    }
}
