using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;

namespace RECO.Reaccommodation_MS.ReaccommodationService.Area
{
    public class ModelService : IModelService
    {
        public async Task<TripMoveAvailabilityRequest> GetMoveavailabilityRequest(BookingDetails bookingDetails,int NextAddDays)
        {
            try
            {
                TripMoveAvailabilityRequest moveAvailabilityRequest = new TripMoveAvailabilityRequest
                {
                    journeyKey = bookingDetails?.Data?.Journeys?[0].JourneyKey,
                    stations = new Stations
                    {
                        destinationStationCodes = new[] { bookingDetails.Data.Journeys[0].Designator.Destination },
                        originStationCodes = new[] { bookingDetails.Data.Journeys[0].Designator.Origin }
                    },
                    dates = new Dates
                    {
                        beginDate = ((DateTime)bookingDetails.Data.Journeys[0].Designator.Departure).AddDays(NextAddDays).ToString(),
                        endDate = ((DateTime)bookingDetails.Data.Journeys[0].Designator.Arrival).AddDays(NextAddDays).ToString()
                    },
                    flightFilters = new FlightFilters
                    {
                        carrierCode = bookingDetails?.Data?.Info?.OwningCarrierCode,
                        maxConnectingFlights = 5
                    },
                    type = 0,
                    passengerMoveType = 0
                };
                return await Task.FromResult(moveAvailabilityRequest);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);

            }
        }

        public async Task<UpdateJourneyModel?> UpdateMoveJourneyRequest(JourneyRequestModel? journeyRequest)
        {
            UpdateJourneyModel? request = null;
            try
            {
                request = new UpdateJourneyModel
                {
                    fromJourneyKey = journeyRequest?.FromJourneykey,
                    toJourneyKey = journeyRequest?.ToJourneykey,
                    fareKey = journeyRequest?.Farekey,
                    standbyPriorityCode = "",
                    inventoryControlType = 0,
                    changeReasonCode = 0,
                    bookingComment = journeyRequest?.BookingComment,
                    adHocConnection = true,
                    adHocIsForGeneralUse = true
                };
                return request;
            }
            catch (Exception)
            {
                return request;
            }
        }
    }
}
