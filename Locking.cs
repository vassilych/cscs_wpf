using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS
{
    
	public class Locking
	{
		public static void Init(SqlConnection sqlConnection)
		{
			//if (sqlConnection.State != System.Data.ConnectionState.Open)
			//{
			//	sqlConnection.Open();
			//}
			//AddLocksTableAndProcedures.CheckAndCreate(sqlConnection);
		}

		private static SqlParameter[] lockParams = new SqlParameter[8];
		private static string GUID = HelperFunctions.GetMACAddress().Substring(0, 12);

		public void Lock()
        {
			//lockParams[0] = new SqlParameter("@DBASE", table.DbName);
			//lockParams[1] = new SqlParameter("@TABLE_NAME", table.Name);
			//lockParams[2] = new SqlParameter("@LOCK_NUM", lockBias);
			//lockParams[3] = new SqlParameter("@GUID", Wbtrv32.lockData.GUID);
			//lockParams[4] = new SqlParameter("@SID", Wbtrv32.lockData.ServiceAgentID);
			//lockParams[5] = new SqlParameter("@CID", Wbtrv32.lockData.ClientIndentifier);
			//lockParams[6] = new SqlParameter("@COMPUTER_USER", Wbtrv32.lockData.ComputerUser);
			//lockParams[7] = new SqlParameter("@SQLWHERE", sqlWhere);
		}

		
	}

	public class HelperFunctions
    {
		//private static string getBiosID()
		//{
		//	// Get BIOS ID
		//	ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
		//	ManagementObjectCollection moc = mos.Get();
		//	string BiosID = "";
		//	foreach (ManagementObject mo in moc)
		//	{
		//		BiosID = mo["SerialNumber"].ToString();
		//	}

		//	return BiosID;
		//}

		//private static string getProcessorID()
		//{
		//	// Get processor ID
		//	ManagementObjectCollection mbsList = null;
		//	ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_processor");
		//	mbsList = mbs.Get();
		//	string ProcessorID = "";
		//	foreach (ManagementObject mo in mbsList)
		//	{
		//		ProcessorID = mo["ProcessorID"].ToString();
		//	}

		//	return ProcessorID;
		//}

		public static string GetMACAddress()
		{
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			String sMacAddress = string.Empty;
			for (int i = 0; i < nics.Length; i++)
			{
				NetworkInterface adapter = nics[i];

				if (sMacAddress == string.Empty)// only return MAC Address from first card  
				{
					IPInterfaceProperties properties = adapter.GetIPProperties();
					return sMacAddress = adapter.GetPhysicalAddress().ToString();
				}
			}
			return sMacAddress;
		}

		private static string getComputerUser()
		{
			string user = null;
			try
			{
				user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
			}
			catch (SecurityException)
			{
				user = null;
			}

			return user;
		}

		private static string getComputerName()
		{
			string name = null;
			try
			{
				name = Environment.MachineName;
			}
			catch (Exception)
			{
				name = null;
			}

			return name;
		}

		private static int getProcessID()
		{
			int processID = -1;
			try
			{
				processID = Process.GetCurrentProcess().Id;
			}
			catch (Exception)
			{
				processID = -1;
			}

			return processID;
		}
	}

	public class AddLocksTableAndProcedures
	{
		public static void CheckAndCreate(SqlConnection sqlConnection)
		{
			string queryString = @"
use adictionary;
SELECT COUNT(*) as countt FROM sysobjects WHERE name='LOCKS' and xtype='U'";

			using (SqlCommand cmd = new SqlCommand(queryString, sqlConnection))
			{
				using (SqlDataReader reader = cmd.ExecuteReader())
				{
					if (reader.HasRows)
					{
						while (reader.Read())
						{
							if (reader["countt"].ToString() == "0")
							{
								reader.Close();
								CreateLocksTable(sqlConnection);
								Create_p_LOCK_Procedure(sqlConnection);
								CreateTruncateLocksProcedure(sqlConnection);
								break;
							}
						}
					}
				}
			}
		}

		private static void CreateLocksTable(SqlConnection sqlConnection)
		{
			string queryString;

			var locksPath = Path.Combine(Directory.GetCurrentDirectory(), "LOCKS.sql");
			if (File.Exists(locksPath))
			{
				queryString = File.ReadAllText(locksPath);
			}
			else
			{
				queryString = @"
CREATE TABLE LOCKS
(
       [DBASE]			[varchar](255)	NOT NULL,
       [TABLE_NAME]		[varchar](20)	NOT NULL,
       [RECORD_ID]		[int]			NOT NULL,
	   [LOCK_TYPE]		[smallint]		NOT NULL,
	   [GUID]			[varchar](12)	NOT NULL,
	   [SID]			[varchar](2)	NULL,
	   [CID]			[smallint]		NULL,
	   [COMPUTER_USER]	[varchar](255)	NOT NULL,
       [LOCK_TIME] [datetime] NOT NULL CONSTRAINT [DF__lock__time]  DEFAULT (getdate())
);

CREATE UNIQUE NONCLUSTERED INDEX [LOCKS_UK] ON LOCKS
(
       [DBASE] ASC,
       [TABLE_NAME] ASC,
       [RECORD_ID] ASC,
	   [GUID] ASC,
	   [COMPUTER_USER] ASC
);
";
			}

			using (SqlCommand cmd = new SqlCommand(queryString, sqlConnection))
			{
				cmd.ExecuteNonQuery();
			}
		}

		private static void Create_p_LOCK_Procedure(SqlConnection sqlConnection)
		{
			string queryString;

			var p_locksPath = Path.Combine(Directory.GetCurrentDirectory(), "p_LOCK.sql");
			if (File.Exists(p_locksPath))
			{
				queryString = File.ReadAllText(p_locksPath);
			}
			else
			{
				queryString = @"

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
EXEC('CREATE PROCEDURE [dbo].[p_LOCK] 
	-- Add the parameters for the stored procedure here
	@DBASE			VARCHAR(255),
	@TABLE_NAME		VARCHAR(20),
	@LOCK_NUM		INT,
	@GUID			VARCHAR(12),
	@SID			VARCHAR(2),
	@CID			SMALLINT,
	@COMPUTER_USER	VARCHAR(255),
	@SQLWHERE		NVARCHAR(MAX),
	@LOCKSTATUS		INT				OUT, -- ((-1) locked by other user, (1) locked, (0) EOF )
	---
	@STATUS			VARCHAR(3)		OUT, -- (OK OR ERR)
	@MESSAGE		VARCHAR(255)	OUT	 -- (ERR msg)	

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @query NVARCHAR(MAX);
	DECLARE @tmpID INT;
	DECLARE @ParmDefinition NVARCHAR(30);

	-- INT.
	SELECT @LOCKSTATUS	= 0;
	SELECT @STATUS		= ''OK'';
	SELECT @MESSAGE		= '''';
	
	BEGIN TRY  	 		
		
		--SELECT RECORD ID						
		select @query = ''SELECT TOP 1 @retvalOUT = ID FROM '' + @DBASE + ''.DBO.'' + @SQLWHERE
		SET @ParmDefinition = N''@retvalOUT int OUTPUT'';

		EXEC SP_EXECUTESQL @query, @ParmDefinition,@retvalOUT=@tmpID OUTPUT
		
		IF (@tmpID IS NULL)
		BEGIN
			
			-- EOF
			SET @LOCKSTATUS = 0; 
			
			RETURN
		END
		
		IF EXISTS 
		(  
			SELECT ''x'' FROM LOCKS  
			WHERE DBASE		= @DBASE  
			AND TABLE_NAME	= @TABLE_NAME  
			AND RECORD_ID	= @tmpID  
			AND(([GUID] = @GUID AND COMPUTER_USER <> @COMPUTER_USER) OR [GUID] <> @GUID) 
		)  
			BEGIN
				
				-- LOCKED BY OTHER USER
				SELECT @LOCKSTATUS = -1; 
				
				RETURN;				   
			END 
			
		-- RECORD IS NOT LOCKED BY OTHER USER. 
		ELSE 
			-- IF RECORD TO BE LOCKED IS NOT ALREADY LOCKED BY ME
			IF NOT EXISTS  
			(  
				SELECT ''x'' FROM LOCKS  
				WHERE DBASE			= @DBASE  
				AND TABLE_NAME		= @TABLE_NAME 
				AND RECORD_ID		= @tmpID 
				AND [GUID]			= @GUID  
				AND COMPUTER_USER	= @COMPUTER_USER  
			) 
				BEGIN  
					
					-- IF EXIST ANY PRIOR LOCK ON CURRENT TABLE, DELETE IT.
					IF(100 = @LOCK_NUM OR 200 = @LOCK_NUM AND EXISTS(SELECT TOP 1 ''x'' FROM LOCKS WHERE DBASE = @DBASE AND TABLE_NAME = @TABLE_NAME AND [GUID] = @GUID AND COMPUTER_USER = @COMPUTER_USER))
						BEGIN  
							
							-- DELETE OLD LOCK 
							DELETE FROM LOCKS 
							WHERE DBASE			= @DBASE 
							AND TABLE_NAME		= @TABLE_NAME 
							AND [GUID]			= @GUID 
							AND COMPUTER_USER	= @COMPUTER_USER;  
						END
					
					-- INSERT NEW LOCK
					INSERT INTO LOCKS (DBASE, TABLE_NAME, RECORD_ID , LOCK_TYPE, [GUID], [SID], CID, COMPUTER_USER) 
					VALUES  (@DBASE, @TABLE_NAME,@tmpID, @LOCK_NUM, @GUID, @SID, @CID, @COMPUTER_USER); 
				END
			
			-- SELECT LOCKED RECORD
			SET @query = ''SELECT TOP 1 * FROM '' + @DBASE + ''.DBO.'' + @TABLE_NAME + '' WITH (NOLOCK) WHERE ID = @ID'';
			SET @ParmDefinition = N''@ID int'';
			EXEC SYS.SP_EXECUTESQL @query, @ParmDefinition, @tmpID;
			
			-- LOCKED BY ME
			SELECT @LOCKSTATUS = 1; 

	END TRY  
	BEGIN CATCH
		 select @STATUS = ''ERR'';
		 select @MESSAGE = SUBSTRING(ERROR_MESSAGE(), 0, 255) + '' Line:'' + ERROR_LINE();
	END CATCH  
END
');
";
			}

			using (SqlCommand cmd = new SqlCommand(queryString, sqlConnection))
			{
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateTruncateLocksProcedure(SqlConnection sqlConnection)
		{
			string queryString;

			var truncLocksPath = Path.Combine(Directory.GetCurrentDirectory(), "TruncateLocks.sql");
			if (File.Exists(truncLocksPath))
			{
				queryString = File.ReadAllText(truncLocksPath);
			}
			else
			{
				queryString = @"
use master;
IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TruncateLocksAdictionary')
BEGIN
    EXEC('
    CREATE PROCEDURE TruncateLocksAdictionary
    AS
    truncate table adictionary.dbo.locks
    ;');
    EXEC SP_PROCOPTION TruncateLocks, 'STARTUP', 'ON'
END;
";
			}

			using (SqlCommand cmd = new SqlCommand(queryString, sqlConnection))
			{
				cmd.ExecuteNonQuery();
			}
		}
	}

}
