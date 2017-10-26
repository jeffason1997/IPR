using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPR
{
    public class Commands
    {
        public static dynamic LoginResponse(string status)
        {
            dynamic response = new
            {
                id = "log in",
                data = new
                {
                    status = status
                }
            };
            return response;
        }

        public static dynamic DoctorLoginResponse(string status)
        {
            dynamic response = new
            {
                id = "doctor/login",
                data = new
                {
                    status = status
                }
            };
            return response;
        }

        public static dynamic DoctorTrainingStartError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/training/start",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }

        public static dynamic DoctorTrianingStopError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/training/stop",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }

        public static dynamic SetPowerError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/setPower",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }

        public static dynamic FollowPatientError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/FollowPatient",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }

        public static dynamic UnFollowPatientError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/UnfollowPatient",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }

        public static dynamic StartAstrandError(string status)
        {
            dynamic Error = new
            {
                id = "doctor/StartAstrand",
                data = new
                {
                    status = "Error",
                    error = status
                }
            };
            return Error;
        }
    }
}

