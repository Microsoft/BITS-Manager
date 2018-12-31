﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Text;
using System.Windows.Controls;

// Set up the BITS namespaces
using BITS = BITSReference1_5;
using BITS10_2 = BITSReference10_2;
using BITS4 = BITSReference4_0;
using BITS5 = BITSReference5_0;

namespace BITSManager
{
    /// <summary>
    /// Interaction logic for JobDetailViewControl.xaml
    /// </summary>
    public partial class JobDetailViewControl : UserControl
    {
        /// <summary>
        /// A JobDetailViewControl is a view into the details of a single BITS job. The BITS job is available
        /// to the main program (for example, to list the files in the job)
        /// </summary>
        public BITS.IBackgroundCopyJob Job { get; internal set; } = null;

        public JobDetailViewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the UI with details from the job
        /// </summary>
        /// <param name="job">The job must always be non-null</param>
        public void SetJob(BITS.IBackgroundCopyJob job)
        {
            Job = job ?? throw new ArgumentNullException("job");

            // Update the details
            BITS.BG_JOB_STATE currState;
            Job.GetState(out currState);

            // time details (Create/Modified/Completed)
            // The DateTime.ToString("F") makes a date + time string in the user's locale.
            BITS._BG_JOB_TIMES times;
            Job.GetTimes(out times);

            var time = MakeDateTime(times.CreationTime);
            _uiJobCreationTime.Text = time > DateTime.MinValue
                ? time.ToString("F")
                : Properties.Resources.JobTimeNotSet;

            time = MakeDateTime(times.ModificationTime);
            _uiJobModificationTime.Text = time > DateTime.MinValue
                ? time.ToString("F")
                : Properties.Resources.JobTimeNotSet;

            time = MakeDateTime(times.TransferCompletionTime);
            _uiJobTransferCompletionTime.Text = time > DateTime.MinValue
                ? time.ToString("F")
                : Properties.Resources.JobTimeNotSet;

            // Progress details (Bytes/Files)
            BITS._BG_JOB_PROGRESS progress;
            Job.GetProgress(out progress);
            var files = String.Format(
                Properties.Resources.JobProgressFileCount,
                progress.FilesTransferred,
                progress.FilesTotal);
            var bytes = progress.BytesTotal == ulong.MaxValue
                ? String.Format(
                    Properties.Resources.JobProgressByteCountUnknown,
                    progress.BytesTransferred)
                : String.Format(
                    Properties.Resources.JobProgressByteCount,
                    progress.BytesTransferred, progress.BytesTotal);

            _uiJobProgressFiles.Text = files;
            _uiJobProgressBytes.Text = bytes;

            // Error details (HRESULT, Context, Description)
            uint NError = 0;
            BITS.IBackgroundCopyError Error;
            BITS.BG_ERROR_CONTEXT ErrorContext;
            int ErrorHRESULT;
            string ErrorContextDescription;
            string ErrorDescription;
            int langid = System.Globalization.CultureInfo.CurrentUICulture.LCID;
            langid = (langid & 0xFFFF); // Equivilent of LANGIDFROMLCID(GetThreadLocale()). BITS takes in the LANGID

            Job.GetErrorCount(out NError);
            if (NError == 0)
            {
                _uiJobErrorCount.Text = Properties.Resources.JobErrorNoErrors;
                _uiJobError.Text = "";
            }
            else // (NError > 0)
            {
                _uiJobErrorCount.Text = NError.ToString("N0"); // Locale-specific numeric with no decimal places

                if (currState != BITS.BG_JOB_STATE.BG_JOB_STATE_ERROR
                    && currState != BITS.BG_JOB_STATE.BG_JOB_STATE_TRANSIENT_ERROR)
                {
                    _uiJobError.Text = Properties.Resources.JobErrorWhenError;
                }
                else
                {
                    try
                    {
                        Job.GetError(out Error);
                        Error.GetError(out ErrorContext, out ErrorHRESULT);
                        Error.GetErrorDescription((uint)langid, out ErrorDescription);
                        Error.GetErrorContextDescription((uint)langid, out ErrorContextDescription);

                        var errorText = String.Format ("\t{0} \t0x{1:X08}\n\t{2} \t{3}{4}\t{5} \t{6}",
                            Properties.Resources.JobErrorHRESULT, 
                            ErrorHRESULT,
                            Properties.Resources.JobErrorDescription, 
                            ErrorDescription,
                            ErrorDescription.EndsWith("\n") ? "" : "\n",
                            Properties.Resources.JobErrorContext, 
                            ErrorContextDescription
                            );
                        _uiJobError.Text = errorText;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        _uiJobError.Text = Properties.Resources.JobErrorException;
                    }
                }
            }

            string jobOwner;
            Job.GetOwner(out jobOwner);

            // convert the user sid to a domain\name
            var identifier = new System.Security.Principal.SecurityIdentifier(jobOwner);
            if (identifier.IsValidTargetType(typeof(System.Security.Principal.NTAccount)))
            {
                string account = identifier.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                _uiJobOwner.Text = account;
            }
            else
            {
                _uiJobOwner.Text = jobOwner;
            }

            // Job priority details
            BITS.BG_JOB_PRIORITY jobPriority;
            Job.GetPriority(out jobPriority);
            _uiJobPriority.Text = BitsConversions.ConvertJobPriorityToString(jobPriority);

            // Job Type details
            BITS.BG_JOB_TYPE jobType;
            Job.GetType(out jobType);
            _uiJobType.Text = BitsConversions.ConvertJobTypeToString(jobType);

            // Job State details
            BITS.BG_JOB_STATE jobState;
            Job.GetState(out jobState);
            _uiJobState.Text = BitsConversions.ConvertJobStateToString(jobState);

            // Values from IBackgroundCopyJob5 Property interface
            // COST_FLAGS, DYNAMIC_CONTENT, HIGH_PERFORMANCE, ON_DEMAND_MODE
            BITS5.IBackgroundCopyJob5 job5 = job as BITS5.IBackgroundCopyJob5;
            if (job5 == null)
            {
                _uiJobCost.Text = Properties.Resources.JobCostNotAvailable;
                _uiJobFlags.Text = Properties.Resources.JobFlagsNotAvailable;
            }
            else
            {
                BITS5.BITS_JOB_PROPERTY_VALUE cost;
                job5.GetProperty(BITS5.BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_ID_COST_FLAGS, out cost);
                var costString = BitsConversions.ConvertCostToString((BitsCosts)cost.Dword);
                _uiJobCost.Text = costString;

                var flagBuilder = new StringBuilder();
                BITS5.BITS_JOB_PROPERTY_VALUE flagValue;
                job5.GetProperty(BITS5.BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_DYNAMIC_CONTENT, out flagValue);
                if (flagValue.Enable != 0)
                {
                    if (flagBuilder.Length > 0)
                    {
                        flagBuilder.Append(", ");
                    }
                    flagBuilder.Append(Properties.Resources.JobFlagsDynamic);
                }

                job5.GetProperty(BITS5.BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_HIGH_PERFORMANCE, out flagValue);
                if (flagValue.Enable != 0)
                {
                    if (flagBuilder.Length > 0)
                    {
                        flagBuilder.Append(", ");
                    }
                    flagBuilder.Append(Properties.Resources.JobFlagsHighPerformance);
                }

                job5.GetProperty(BITS5.BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_ON_DEMAND_MODE, out flagValue);
                if (flagValue.Enable != 0)
                {
                    if (flagBuilder.Length > 0)
                    {
                        flagBuilder.Append(", ");
                    }
                    flagBuilder.Append(Properties.Resources.JobFlagsOnDemandMode);
                }

                if (flagBuilder.Length == 0)
                {
                    flagBuilder.Append(Properties.Resources.JobFlagsNoneSet);
                }
                _uiJobFlags.Text = flagBuilder.ToString();
            }

            // Get the JobHttpOptions custom method
            var httpOptions2 = Job as BITS10_2.IBackgroundCopyJobHttpOptions2;

            if (httpOptions2 == null)
            {
                _uiJobHttpMethod.Text = Properties.Resources.JobHttpMethodNotAvailable;
            }
            else
            {
                string httpMethod;
                try
                {
                    httpOptions2.GetHttpMethod(out httpMethod);
                    _uiJobHttpMethod.Text = httpMethod ?? Properties.Resources.JobHttpMethodNotSet;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    _uiJobHttpMethod.Text = String.Format(Properties.Resources.JobHttpMethodException, ex.Message);
                }
            }

            // Get the HttpJobOptions
            var httpOptions = Job as BITS4.IBackgroundCopyJobHttpOptions;
            if (httpOptions == null)
            {
                _uiJobCustomHeaders.Text = Properties.Resources.JobCustomHeadersNotAvailable;
            }
            else
            {
                string customHeaders;
                try
                {
                    httpOptions.GetCustomHeaders(out customHeaders);
                    var headers = customHeaders ?? Properties.Resources.JobCustomHeadersNotSet;
                    headers = TabifyHttpHeaders.AddTabs(headers);
                    _uiJobCustomHeaders.Text = headers;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    _uiJobCustomHeaders.Text = String.Format(
                        Properties.Resources.JobCustomHeadersException,
                        ex.Message);
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    _uiJobCustomHeaders.Text = String.Format(
                        Properties.Resources.JobCustomHeadersException,
                        ex.Message);
                }
            }

            // Update the list of files associated with the job.
            ListBITSJobFiles(Job);
        }

        /// <summary>
        /// Converts a Windows style FILETIME value into a .NET DateTime
        /// </summary>
        /// <param name="value">FILETIME from a BITS _BG_JOB_TIMES</param>
        /// <returns>DateTime based on the local (not UTC) FILETIME
        /// or DateTime.MinValue for FILETIMES that are all-zeros</returns>
        private static DateTime MakeDateTime(BITS._FILETIME value)
        {
            long ticks = ((long)value.dwHighDateTime << 32) + (long)value.dwLowDateTime;
            return ticks == 0 ? DateTime.MinValue : DateTime.FromFileTime(ticks);
        }

        private void ListBITSJobFiles(BITS.IBackgroundCopyJob Job)
        {
            _uiFileList.Items.Clear();

            // Iterate through the jobs
            BITS.IEnumBackgroundCopyFiles filesEnum;
            Job.EnumFiles(out filesEnum);

            uint nfilesFetched = 0;
            BITS.IBackgroundCopyFile file = null;

            do
            {
                filesEnum.Next(1, out file, ref nfilesFetched);
                if (nfilesFetched > 0)
                {
                    var control = new FileDetailViewControl(Job, file);
                    _uiFileList.Items.Add(control);
                }
            }
            while (nfilesFetched > 0);
        }
    }
}
