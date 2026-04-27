using Newtonsoft.Json;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.Models.ResponseModel.BookingModel;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.Models.ResponseModel;
using AutoMapper;
using RECO.Reaccommodation_MS.Models.Enum;
using RECO.Reaccommodation_MS.Models;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.Models.ResponseModel.MCT;

namespace RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService
{
    public class ReaccommodationHandlerService : IReaccommodationHandlerService
    {
        private readonly IMultipleJourneyService _multipleJourneyService;
        private readonly ISingleJourneyService _singleJourneyService;
        private readonly ISpecificFlightJourneyService _specificJourneyService;
        private readonly IBookingService _bookingService;
        private readonly ILogHelper _logHelper;
        private readonly IMapper _mapper;
        private readonly IJourneyService _journeyService;
        private readonly string _caller = typeof(ReaccommodationHandlerService).Name;

        public ReaccommodationHandlerService(IBookingService bookingService, ILogHelper logHelper,IMapper mapper, ISpecificFlightJourneyService specificJourneyService,
            IMultipleJourneyService multipleJourneyService, ISingleJourneyService singleJourneyService, IJourneyService journeyService)
        {
            _bookingService = bookingService;
            _logHelper = logHelper;
            _multipleJourneyService = multipleJourneyService;
            _singleJourneyService = singleJourneyService;
            _specificJourneyService = specificJourneyService;
            _mapper = mapper;
            _journeyService = journeyService;   
        }
        /// <summary>
        ///  Reaccommodate disturbed flight to the specific flight number.
        /// </summary>
        /// <param name="_listOfPNR"></param>
        /// <param name="_moveToJourneykey"></param>
        /// <param name="newIdentifier"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<PNRDetails> ReaccomodateToSpecificFlight(Reaccommodation_Model reaccommodationMs, string Token)
        {
            try
            {
                await _logHelper.LogInfo($"{_caller}:{":- ReaccomodateToGivenFlight"} :- {"Start"}");
                PNRDetails pnrDetails = new PNRDetails();
                pnrDetails.pnrDetail = new List<PNRDetail>();
                pnrDetails.routingDetail = new List<RoutingDetail>();
                pnrDetails.disruptedFlightDB = reaccommodationMs.disruptedFlightDB;
                for (int i = 0; i < reaccommodationMs?.PNRDetailList?.Count; i++)
                {
                    reaccommodationMs.PNRDetail = reaccommodationMs.PNRDetailList[i];
                    PNRDetails _pnrdata = await AccommodationByFlightNo(reaccommodationMs, Token);
                    pnrDetails.pnrDetail.AddRange(_pnrdata.pnrDetail);
                    pnrDetails.routingDetail.AddRange(_pnrdata.routingDetail);
                }
                List<PNRDetail> _againcheckPNR = pnrDetails.pnrDetail.Where(x => x.ReasonForFailure == Enum_PNR.NavitaireFailure.ToString() || x.ReasonForFailure == Enum_PNR.NotAbleToFetchDetails.ToString() || x.ReasonForFailure == Enum_PNR.UpdateMoveJourneyFailed.ToString()).ToList();
                if (_againcheckPNR.Count() > 0)
                {
                    pnrDetails.pnrDetail.RemoveAll(x => x.ReasonForFailure == Enum_PNR.NavitaireFailure.ToString()|| x.ReasonForFailure == Enum_PNR.NotAbleToFetchDetails.ToString() || x.ReasonForFailure == Enum_PNR.UpdateMoveJourneyFailed.ToString());
                    var validPNRIDs = new HashSet<int>(pnrDetails.pnrDetail.Select(x => x.PNRID));
                    pnrDetails.routingDetail.RemoveAll(x => !validPNRIDs.Contains(x.PNRID));
                    for (int i = 0; i < _againcheckPNR.Count; i++)
                    {
                        reaccommodationMs.PNRDetail = _againcheckPNR[i];
                        PNRDetails _pnrdata = await AccommodationByFlightNo(reaccommodationMs, Token);
                        pnrDetails.pnrDetail.AddRange(_pnrdata.pnrDetail);
                        pnrDetails.routingDetail.AddRange(_pnrdata.routingDetail);
                    }
                }
                return pnrDetails;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{":- ReaccomodateToSpecificFlight"}{" msg:-"} {ex.Message}:- {"End "}");
                await _logHelper.LogConsoleException(ex);
                throw new InvalidOperationException(ex.Message);
            }
        }
        /// <summary>
        /// Reaccomodate by ReaccomodateToSuitableFlight 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<PNRDetails> ReaccomodateToSuitableFlight(Reaccommodation_Model reaccommodationMs, string Token)
        {
            await _logHelper.LogInfo($"{_caller}:{":- ReaccomodateToSuitableFlight"} :- {"Start"}");
            PNRDetails pNRandAlternateFlightList = new PNRDetails();
            pNRandAlternateFlightList.pnrDetail = new List<PNRDetail>();
            pNRandAlternateFlightList.routingDetail = new List<RoutingDetail>();
            PNRDetail pNRTableModel1 = new PNRDetail();
            try
            {
                pNRTableModel1 = reaccommodationMs?.PNRDetailList?.FirstOrDefault(x => x.PNRCode == reaccommodationMs.sortedPNR.PNRCode);
                var bookingDetails = await _bookingService.GetBookingByRecordLocator(reaccommodationMs?.sortedPNR?.PNRCode??"", Token);
                if (bookingDetails != null && bookingDetails != "")
                {
                    reaccommodationMs.bookingDetails = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                    reaccommodationMs.EntireBooking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                    reaccommodationMs.impactedFlight = new MCT_ImpactedFlight();
                    if (reaccommodationMs.bookingDetails != null && reaccommodationMs.bookingDetails.Data != null && reaccommodationMs.bookingDetails?.Data?.Journeys?.Count > 0)
                    {
                        reaccommodationMs.PNRDetail = pNRTableModel1;
                        if (reaccommodationMs.bookingDetails.Data.Journeys.Count() > 1)
                        {
                            pNRandAlternateFlightList = await _multipleJourneyService.SuitableflightforMultiplejourneys(reaccommodationMs, Token);
                        }
                        else
                        {
                            pNRandAlternateFlightList = await _singleJourneyService.SuitableflightforSingleJourneys(reaccommodationMs, Token);
                        }
                    }
                }
                return pNRandAlternateFlightList;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}:{":- ReaccomodateToSuitableFlight"} :- {ex.Message}");
                pNRandAlternateFlightList = await _journeyService.GetAccommodationByFlightNoResponseModel(null,null, pNRTableModel1, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                return pNRandAlternateFlightList;
            }
        }

        private async Task<PNRDetails> AccommodationByFlightNo(Reaccommodation_Model reaccommodationMs, string Token)
        {
            PNRDetails pnrDetails = new PNRDetails();
            pnrDetails.pnrDetail = new List<PNRDetail>();
            pnrDetails.routingDetail = new List<RoutingDetail>();
            try
            {
                var bookingDetails = await _bookingService.GetBookingByRecordLocator(reaccommodationMs?.PNRDetail?.PNRCode??"", Token);
                if (bookingDetails != null && bookingDetails != "")
                {
                    reaccommodationMs.bookingDetails = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                    reaccommodationMs.EntireBooking = JsonConvert.DeserializeObject<BookingDetails>(bookingDetails);
                    if (reaccommodationMs?.bookingDetails != null && reaccommodationMs?.bookingDetails?.Data != null && reaccommodationMs?.bookingDetails?.Data?.Journeys?.Count()>0)
                    {
                        if (reaccommodationMs?.bookingDetails?.Data?.Journeys?.Count() > 1)
                        {
                            pnrDetails= await _specificJourneyService.SpecificflightforMultiplejourneys(reaccommodationMs, Token);
                        }
                        else
                        {
                            pnrDetails= await _specificJourneyService.SpecificflightforSingleJourneys(reaccommodationMs, Token);
                        }
                    }
                }
                return pnrDetails;
            }
            catch (Exception ex)
            {
                await _logHelper.LogError($"{_caller}{":- AccommodationByFlightNo"}{" msg:-"} {ex.Message}:- {"End "}{":- PNR -: "}{reaccommodationMs?.PNRDetail?.PNRCode ?? ""}");
                pnrDetails = await _journeyService.GetAccommodationByFlightNoResponseModel(null,null, reaccommodationMs?.PNRDetail, false, Enum_PNR.NotAbleToFetchDetails.ToString());
                return pnrDetails;
            }
        }
    }
}
