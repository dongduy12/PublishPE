using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Microsoft.EntityFrameworkCore;
using API_WEB.ModelsOracle;
using API_WEB.ModelsDB;

namespace API_WEB.Controllers.SmartFA
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckInOutController : ControllerBase
    {
        private readonly OracleDbContext _oracleContext;
        private readonly CSDL_NE _sqlContext;
        public CheckInOutController(OracleDbContext oracleContext, CSDL_NE sqlContext)
        {
            _oracleContext = oracleContext;
            _sqlContext = sqlContext;
        }

        // Use a distinct request model name to avoid Swagger schema clashes
        public class CheckInOutRequest
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class CheckInRecord
        {
            public string SERIAL_NUMBER { get; set; } = string.Empty;
            public string MO_NUMBER { get; set; } = string.Empty;
            public string MODEL_NAME { get; set; } = string.Empty;
            public string STATION_NAME { get; set; } = string.Empty;
            public string ERROR_CODE { get; set; } = string.Empty;
            public DateTime? IN_DATETIME { get; set; }
            public string ERROR_DESC { get; set; } = string.Empty;
        }

        public class CheckOutRecord
        {
            public string SERIAL_NUMBER { get; set; } = string.Empty;
            public string MODEL_NAME { get; set; } = string.Empty;
            public string PRODUCT_LINE { get; set; } = string.Empty;
            public string P_SENDER { get; set; } = string.Empty;
            public string REPAIRER { get; set; } = string.Empty;
            public DateTime? IN_DATETIME { get; set; }
            public DateTime? OUT_DATETIME { get; set; }
            public string REMARK { get; set; } = string.Empty;
            public string ERROR_DESC { get; set; } = string.Empty;
            public string CHECKIN_STATUS { get; set; } = string.Empty;
        }

        [HttpPost("GetCheckInOut")]
        public async Task<IActionResult> GetCheckInOut([FromBody] CheckInOutRequest request)
        {
            await using var connection = new OracleConnection(_oracleContext.Database.GetDbConnection().ConnectionString);
            try
            {
                await connection.OpenAsync();

                var checkInQuery = @"SELECT a.SERIAL_NUMBER,
                                            a.MO_NUMBER,
                                            a.MODEL_NAME,
                                            a.STATION_NAME,
                                            a.REMARK AS ERROR_CODE,
                                            a.IN_DATETIME,
                                            c.ERROR_DESC
                                       FROM sfism4.r_repair_in_out_t a
                                       INNER JOIN sfis1.c_model_desc_t b ON a.model_name = b.model_name
                                       INNER JOIN sfis1.c_error_code_t c ON a.REMARK = c.ERROR_CODE
                                       WHERE b.MODEL_SERIAL = 'ADAPTER'
                                         AND a.P_SENDER IN ('V0904136','V0945375','V0928908')
                                         AND a.STATION_NAME != 'REPAIR_B36R'
                                         AND a.IN_DATETIME BETWEEN :startDate AND :endDate
                                         AND ( (a.MODEL_NAME NOT LIKE '900%' AND a.MODEL_NAME NOT LIKE '692%' AND a.MODEL_NAME NOT LIKE '699%')
                                               OR EXISTS (SELECT 1 FROM SFISM4.R109 d WHERE d.SERIAL_NUMBER = a.SERIAL_NUMBER AND d.REASON_CODE LIKE '%B36R%'))";
                var checkInList = new List<CheckInRecord>();
                await using (var cmd = new OracleCommand(checkInQuery, connection))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter(":startDate", OracleDbType.Date) { Value = request.StartDate });
                    cmd.Parameters.Add(new OracleParameter(":endDate", OracleDbType.Date) { Value = request.EndDate });
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        checkInList.Add(new CheckInRecord
                        {
                            SERIAL_NUMBER = reader["SERIAL_NUMBER"].ToString() ?? string.Empty,
                            MO_NUMBER = reader["MO_NUMBER"].ToString() ?? string.Empty,
                            MODEL_NAME = reader["MODEL_NAME"].ToString() ?? string.Empty,
                            STATION_NAME = reader["STATION_NAME"].ToString() ?? string.Empty,
                            ERROR_CODE = reader["ERROR_CODE"].ToString() ?? string.Empty,
                            IN_DATETIME = reader["IN_DATETIME"] as DateTime?,
                            ERROR_DESC = reader["ERROR_DESC"].ToString() ?? string.Empty
                        });
                    }
                }

                var checkOutQuery = @"SELECT a.SERIAL_NUMBER,
                                            a.MODEL_NAME,
                                            b.PRODUCT_LINE,
                                            a.P_SENDER,
                                            a.REPAIRER,
                                            a.IN_DATETIME,
                                            a.OUT_DATETIME,
                                            a.REMARK,
                                            c.ERROR_DESC,
                                            CASE WHEN TRUNC(a.IN_DATETIME) = TRUNC(:startDate) THEN 'CHECKIN_TRONG_NGAY' ELSE 'CHECKIN_TRUOC_DO' END AS CHECKIN_STATUS
                                       FROM sfism4.r_repair_in_out_t a
                                       INNER JOIN sfis1.c_model_desc_t b ON a.model_name = b.model_name
                                       INNER JOIN sfis1.c_error_code_t c ON a.REMARK = c.ERROR_CODE
                                       WHERE b.MODEL_SERIAL = 'ADAPTER'
                                         AND a.P_SENDER IN ('V0904136','V0945375','V0928908')
                                         AND a.STATION_NAME != 'REPAIR_B36R'
                                         AND a.MODEL_NAME NOT LIKE '900%'
                                         AND a.MODEL_NAME NOT LIKE '692%'
                                         AND a.MODEL_NAME NOT LIKE '699%'
                                         AND a.REPAIRER IS NOT NULL
                                         AND a.OUT_DATETIME BETWEEN :startDate AND :endDate";
                var checkOutList = new List<CheckOutRecord>();
                await using (var cmd = new OracleCommand(checkOutQuery, connection))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter(":startDate", OracleDbType.Date) { Value = request.StartDate });
                    cmd.Parameters.Add(new OracleParameter(":endDate", OracleDbType.Date) { Value = request.EndDate });
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        checkOutList.Add(new CheckOutRecord
                        {
                            SERIAL_NUMBER = reader["SERIAL_NUMBER"].ToString() ?? string.Empty,
                            MODEL_NAME = reader["MODEL_NAME"].ToString() ?? string.Empty,
                            PRODUCT_LINE = reader["PRODUCT_LINE"].ToString() ?? string.Empty,
                            P_SENDER = reader["P_SENDER"].ToString() ?? string.Empty,
                            REPAIRER = reader["REPAIRER"].ToString() ?? string.Empty,
                            IN_DATETIME = reader["IN_DATETIME"] as DateTime?,
                            OUT_DATETIME = reader["OUT_DATETIME"] as DateTime?,
                            REMARK = reader["REMARK"].ToString() ?? string.Empty,
                            ERROR_DESC = reader["ERROR_DESC"].ToString() ?? string.Empty,
                            CHECKIN_STATUS = reader["CHECKIN_STATUS"].ToString() ?? string.Empty
                        });
                    }
                }

                var exportList = await _sqlContext.Exports
                    .Where(e => e.CheckingB36R == true && e.ExportDate >= request.StartDate && e.ExportDate <= request.EndDate)
                    .Select(e => e.SerialNumber)
                    .ToListAsync();

                var response = new
                {
                    checkIn = new { count = checkInList.Count, data = checkInList },
                    checkOut = new { count = checkOutList.Count + exportList.Count, data = checkOutList, exportCount = exportList.Count, exportSerials = exportList }
                };

                return Ok(response);
            }
            catch (OracleException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
