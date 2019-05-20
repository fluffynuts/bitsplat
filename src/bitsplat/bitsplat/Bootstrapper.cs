using Dapper;

namespace bitsplat
{
    public class Bootstrapper
    {
        public void Init()
        {
            SqlMapper.AddTypeHandler(new DateTimeHandler());
        }
    }
}