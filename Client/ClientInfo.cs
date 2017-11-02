using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class ClientInfo
    {
        public string UserName { get; }
        public int Age { get; }
        public Sex sex { get;}
        public int Weight { get; }

        public ClientInfo(string name, int age, Sex s, int weight)
        {
            UserName = name;
            Age = age;
            sex = s;
            Weight = weight;
        }

        public override string ToString()
        {
            return UserName;
        }
    }

    public class ClientList : List<ClientInfo>
    {
       
    }

    public enum Sex
    {
        Male,Female,NotConfirmedYet
    }
}
