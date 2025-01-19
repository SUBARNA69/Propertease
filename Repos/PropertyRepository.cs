using Microsoft.Data.SqlClient;
using Propertease.Models;

public class PropertyRepository
{
    private readonly string _connectionString;

    public PropertyRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("dbConn");
    }

    public async Task<PropertyDetailsViewModel> GetPropertyDetails(int propertyId)
    {
        string query = @"
    SELECT 
        p.[Photo] AS ImageUrl,
        s.[FullName] AS SellerName,
        s.[ContactNumber] AS SellerContact,
        p.PropertyType,
        h.Bedrooms,
        h.Kitchens,
        h.SittingRooms,
        h.Bathrooms,
        h.Floors,
        h.Area,
        h.FacingDirection,
        a.Rooms,
        a.RoomSize,
        l.Area
    FROM Properties p
    LEFT JOIN [Users] s ON p.SellerId = s.Id
    LEFT JOIN Houses h ON p.Id = h.PropertyId
    LEFT JOIN Apartments a ON p.Id = a.PropertyId
    LEFT JOIN Lands l ON p.Id = l.PropertyId
    WHERE p.Id = @PropertyId";

        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PropertyId", propertyId);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new PropertyDetailsViewModel
                    {
                        ImageUrl = reader["ImageUrl"].ToString(),
                        SellerName = reader["SellerName"].ToString(),
                        SellerContact = reader["SellerContact"].ToString(),
                        PropertyType = reader["PropertyType"].ToString(),
                        Bedrooms = reader["Bedrooms"] != DBNull.Value ? Convert.ToInt32(reader["Bedrooms"]) : (int?)null,
                        Kitchens = reader["Kitchens"] != DBNull.Value ? Convert.ToInt32(reader["Kitchens"]) : (int?)null,
                        SittingRooms = reader["SittingRooms"] != DBNull.Value ? Convert.ToInt32(reader["SittingRooms"]) : (int?)null,
                        Bathrooms = reader["Bathrooms"] != DBNull.Value ? Convert.ToInt32(reader["Bathrooms"]) : (int?)null,
                        Floors = reader["Floors"] != DBNull.Value ? Convert.ToInt32(reader["Floors"]) : (int?)null,
                        Area = reader["Area"] != DBNull.Value ? Convert.ToDouble(reader["Area"]) : (double?)null,
                        FacingDirection = reader["FacingDirection"].ToString(),
                        Rooms = reader["Rooms"] != DBNull.Value ? Convert.ToInt32(reader["Rooms"]) : (int?)null,
                        RoomSize = reader["RoomSize"] != DBNull.Value ? Convert.ToDouble(reader["RoomSize"]) : (double?)null,
                        LandArea = reader["Area"] != DBNull.Value ? Convert.ToDouble(reader["Area"]) : (double?)null,
                    };
                }
            }
        }

        return null;
    }


}
