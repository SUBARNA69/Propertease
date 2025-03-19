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
            p.Id,
            p.Title,
            p.Description,
            p.Price,
            p.City,
            p.District,
            p.Province,
            p.PropertyType,
            p.Status,
            p.RoadAccess,
            p.Latitude,
            p.Longitude,
            p.CreatedAt,
            p.ThreeDModel,
            p.SellerId,
            s.FullName AS SellerName,
            s.ContactNumber AS SellerContact,
            s.Email AS SellerEmail,
            s.Address AS SellerAddress,
            h.Bedrooms,
            h.Kitchens,
            h.SittingRooms,
            h.Bathrooms,
            h.Floors,
            h.LandArea AS HouseLandArea,
            h.BuildupArea,
            h.BuiltYear AS HouseBuiltYear,
            h.FacingDirection,
            a.Rooms,
            a.RoomSize,
            a.BuiltYear AS ApartmentBuiltYear,
            l.LandArea AS LandLandArea,
            l.LandType,
            l.SoilQuality,
            pi.Photo AS ImageUrl
        FROM Properties p
        LEFT JOIN Users s ON p.SellerId = s.Id
        LEFT JOIN Houses h ON p.Id = h.PropertyID
        LEFT JOIN Apartments a ON p.Id = a.PropertyID
        LEFT JOIN Lands l ON p.Id = l.PropertyID
        LEFT JOIN PropertyImages pi ON p.Id = pi.PropertyId
        WHERE p.Id = @PropertyId";

        using (var connection = new SqlConnection(_connectionString))
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PropertyId", propertyId);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();

                PropertyDetailsViewModel propertyDetails = null;
                var imageUrls = new List<string>();

                while (await reader.ReadAsync())
                {
                    if (propertyDetails == null)
                    {
                        propertyDetails = new PropertyDetailsViewModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            City = reader["City"].ToString(),
                            District = reader["District"].ToString(),
                            Province = reader["Province"].ToString(),
                            PropertyType = reader["PropertyType"].ToString(),
                            Status = reader["Status"].ToString(),
                            RoadAccess = reader["RoadAccess"].ToString(),
                            Latitude = reader["Latitude"] != DBNull.Value ? Convert.ToDouble(reader["Latitude"]) : (double?)null,
                            Longitude = reader["Longitude"] != DBNull.Value ? Convert.ToDouble(reader["Longitude"]) : (double?)null,
                            CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.Now,
                            ThreeDModel = reader["ThreeDModel"] != DBNull.Value ? reader["ThreeDModel"].ToString() : null,
                            SellerId = reader["SellerId"] != DBNull.Value ? Convert.ToInt32(reader["SellerId"]) : 0,
                            SellerName = reader["SellerName"] != DBNull.Value ? reader["SellerName"].ToString() : null,
                            SellerContact = reader["SellerContact"] != DBNull.Value ? reader["SellerContact"].ToString() : null,
                            SellerEmail = reader["SellerEmail"] != DBNull.Value ? reader["SellerEmail"].ToString() : null,
                            SellerAddress = reader["SellerAddress"] != DBNull.Value ? reader["SellerAddress"].ToString() : null,

                            // Property type specific fields
                            Bedrooms = reader["Bedrooms"] != DBNull.Value ? Convert.ToInt32(reader["Bedrooms"]) : (int?)null,
                            Kitchens = reader["Kitchens"] != DBNull.Value ? Convert.ToInt32(reader["Kitchens"]) : (int?)null,
                            SittingRooms = reader["SittingRooms"] != DBNull.Value ? Convert.ToInt32(reader["SittingRooms"]) : (int?)null,
                            Bathrooms = reader["Bathrooms"] != DBNull.Value ? Convert.ToInt32(reader["Bathrooms"]) : (int?)null,
                            Floors = reader["Floors"] != DBNull.Value ? Convert.ToInt32(reader["Floors"]) : (int?)null,
                            FacingDirection = reader["FacingDirection"] != DBNull.Value ? reader["FacingDirection"].ToString() : null,
                            Rooms = reader["Rooms"] != DBNull.Value ? Convert.ToInt32(reader["Rooms"]) : (int?)null,
                            RoomSize = reader["RoomSize"] != DBNull.Value ? Convert.ToDouble(reader["RoomSize"]) : (double?)null,

                            // Handle different LandArea fields based on property type
                            LandArea = propertyDetails?.PropertyType == "House" ?
                                (reader["HouseLandArea"] != DBNull.Value ? Convert.ToDouble(reader["HouseLandArea"]) : (double?)null) :
                                (reader["LandLandArea"] != DBNull.Value ? Convert.ToDouble(reader["LandLandArea"]) : (double?)null),

                            BuildupArea = reader["BuildupArea"] != DBNull.Value ? Convert.ToDouble(reader["BuildupArea"]) : (double?)null,

                            // Handle different BuiltYear fields based on property type
                            BuiltYear = propertyDetails?.PropertyType == "House" ?
                                (reader["HouseBuiltYear"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["HouseBuiltYear"])) : DateOnly.MinValue) :
                                (reader["ApartmentBuiltYear"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["ApartmentBuiltYear"])) : DateOnly.MinValue),
                            LandType = reader["LandType"] != DBNull.Value ? reader["LandType"].ToString() : null,
                            SoilQuality = reader["SoilQuality"] != DBNull.Value ? reader["SoilQuality"].ToString() : null,
                        };
                    }

                    // Collect all image URLs
                    if (reader["ImageUrl"] != DBNull.Value)
                    {
                        imageUrls.Add(reader["ImageUrl"].ToString());
                    }
                }

                if (propertyDetails != null)
                {
                    propertyDetails.ImageUrl = imageUrls; // Ensure this matches the property name in the ViewModel

                    // Set LandArea based on property type after the object is created
                    if (propertyDetails.PropertyType == "House")
                    {
                        propertyDetails.LandArea = reader["HouseLandArea"] != DBNull.Value ?
                            Convert.ToDouble(reader["HouseLandArea"]) : (double?)null;
                    }
                    else if (propertyDetails.PropertyType == "Land")
                    {
                        propertyDetails.LandArea = reader["LandLandArea"] != DBNull.Value ?
                            Convert.ToDouble(reader["LandLandArea"]) : (double?)null;
                    }
                }

                return propertyDetails;
            }
        }
    }
}