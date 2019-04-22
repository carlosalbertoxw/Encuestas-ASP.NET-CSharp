using System;

namespace Model
{
    public class Poll
    {
        public Int32 Id { get; set; }
        public String Title { get; set; }
        public Int32 Position { get; set; }
        public String Description { get; set; }
        public User User { get; set; }
    }
}
