﻿using System;

namespace Labels.Models
{
    public class UserItem
    {
        public int ClientID { get; set; }
        public string LName { get; set; }
        public string FName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime? StartDate { get; set; }
    }
}