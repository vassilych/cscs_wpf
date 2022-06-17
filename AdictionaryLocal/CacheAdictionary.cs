using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WpfCSCS.Adictionary;

namespace WpfCSCS.AdictionaryLocal
{
    public class CacheAdictionary
    {
        public static List<SY_DATABASES> GetSY_DATABASES(SqlConnection myConnection)
        {

            try
            {
                List<SY_DATABASES> list = null;

                string queryString = $@"
SELECT 
	[ID]
	,[SYCD_USERCODE]
	,[SYCD_COMPCODE]
	,[SYCD_YEAR]
	,[SYCD_DESCRIPTION]
	,[SYCD_DBASENAME]
	,[SYCD_USERNAME]
	,[SYCD_USERPSWD]
  FROM [adictionary].[dbo].[SY_DATABASES]
";
                using (SqlCommand cmd = new SqlCommand(queryString, myConnection))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            list = new List<SY_DATABASES>();

                            while (dr.Read())
                            {
                                var row = new SY_DATABASES();

                                row.SYCD_ID = (int)dr["ID"];
                                row.SYCD_USERCODE = dr["SYCD_USERCODE"].ToString().TrimEnd();
                                row.SYCD_COMPCODE = dr["SYCD_COMPCODE"].ToString().TrimEnd();
                                row.SYCD_YEAR = dr["SYCD_YEAR"].ToString().TrimEnd();
                                row.SYCD_DESCRIPTION = dr["SYCD_DESCRIPTION"].ToString().TrimEnd();
                                row.SYCD_DBASENAME = dr["SYCD_DBASENAME"].ToString().TrimEnd();
                                row.SYCD_USERNAME = dr["SYCD_USERNAME"].ToString().TrimEnd();
                                row.SYCD_USERPSWD = dr["SYCD_USERPSWD"].ToString().TrimEnd();

                                list.Add(row);
                            }


                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<SY_FIELDS> GetSY_FIELDS(SqlConnection myConnection)
        {

            try
            {
                List<SY_FIELDS> list = null;

                string queryString = $@"
SELECT 
    [ID]
    ,[SYTD_SCHEMA]
    ,[SYTD_FIELD]
    ,[SYTD_DESCRIPTION]
    ,[SYTD_TYPE]
    ,[SYTD_SIZE]
    ,[SYTD_DEC]
    ,[SYTD_CASE]
    ,[SYTD_ARRAYNUM]
  FROM [adictionary].[dbo].[SY_FIELDS]
";
                using (SqlCommand cmd = new SqlCommand(queryString, myConnection))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            list = new List<SY_FIELDS>();

                            while (dr.Read())
                            {
                                var row = new SY_FIELDS();

                                row.SYTD_ID = (int)dr["ID"];
                                row.SYTD_SCHEMA = dr["SYTD_SCHEMA"].ToString().TrimEnd();
                                row.SYTD_FIELD = dr["SYTD_FIELD"].ToString().TrimEnd();
                                row.SYTD_DESCRIPTION = dr["SYTD_DESCRIPTION"].ToString().TrimEnd();
                                row.SYTD_TYPE = dr["SYTD_TYPE"].ToString().TrimEnd();
                                row.SYTD_SIZE = (int)dr["SYTD_SIZE"];
                                row.SYTD_DEC = (int)dr["SYTD_DEC"];
                                row.SYTD_CASE = dr["SYTD_CASE"].ToString().TrimEnd();
                                row.SYTD_ARRAYNUM = (int)dr["SYTD_ARRAYNUM"];

                                list.Add(row);
                            }


                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<SY_INDEXES> GetSY_INDEXES(SqlConnection myConnection)
        {

            try
            {
                List<SY_INDEXES> list = null;

                string queryString = $@"
SELECT 
	[ID]
	,[SYKI_SCHEMA]
	,[SYKI_DESCRIPTION]
	,[SYKI_KEYNAME]
	,[SYKI_KEYNUM]
	,[SYKI_FIELD]
	,[SYKI_UNIQUE]
	,[SYKI_ASCDESC]
	,[SYKI_SEGNUM]
  FROM [adictionary].[dbo].[SY_INDEXES]
";
                using (SqlCommand cmd = new SqlCommand(queryString, myConnection))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            list = new List<SY_INDEXES>();

                            while (dr.Read())
                            {
                                var row = new SY_INDEXES();

                                row.SYKI_ID = (int)dr["ID"];
                                row.SYKI_SCHEMA = dr["SYKI_SCHEMA"].ToString().TrimEnd();
                                row.SYKI_DESCRIPTION = dr["SYKI_DESCRIPTION"].ToString().TrimEnd();
                                row.SYKI_KEYNAME = dr["SYKI_KEYNAME"].ToString().TrimEnd();
                                row.SYKI_KEYNUM = (int)dr["SYKI_KEYNUM"];
                                row.SYKI_FIELD = dr["SYKI_FIELD"].ToString().TrimEnd();
                                row.SYKI_UNIQUE = dr["SYKI_UNIQUE"].ToString().TrimEnd();
                                row.SYKI_ASCDESC = dr["SYKI_ASCDESC"].ToString().TrimEnd();
                                row.SYKI_SEGNUM = (int)dr["SYKI_SEGNUM"];

                                list.Add(row);
                            }


                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static List<SY_TABLES> GetSY_TABLES(SqlConnection myConnection)
        {

            try
            {
                List<SY_TABLES> list = null;

                string queryString = $@"
SELECT 
	[ID]
	,[SYCT_NAME]
	,[SYCT_SCHEMA]
	,[SYCT_DESCRIPTION]
	,[SYCT_TYPE]
	,[SYCT_USERCODE]
  FROM [adictionary].[dbo].[SY_TABLES]
";
                using (SqlCommand cmd = new SqlCommand(queryString, myConnection))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            list = new List<SY_TABLES>();

                            while (dr.Read())
                            {
                                var row = new SY_TABLES();

                                row.SYCT_ID = (int)dr["ID"];
                                row.SYCT_NAME = dr["SYCT_NAME"].ToString().TrimEnd();
                                row.SYCT_SCHEMA = dr["SYCT_SCHEMA"].ToString().TrimEnd();
                                row.SYCT_DESCRIPTION = dr["SYCT_DESCRIPTION"].ToString().TrimEnd();
                                row.SYCT_TYPE = dr["SYCT_TYPE"].ToString().TrimEnd();
                                row.SYCT_USERCODE = dr["SYCT_USERCODE"].ToString().TrimEnd();

                                list.Add(row);
                            }


                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }


}
