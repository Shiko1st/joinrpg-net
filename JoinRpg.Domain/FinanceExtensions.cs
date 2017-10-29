﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{

    /// <summary>
    /// Compares two instances of ProjectFeeSetting.
    /// First compares dates, then Ids.
    /// </summary>
    public class ProjectFeeSettingComparer: IComparer<ProjectFeeSetting>
    {
        public int Compare(ProjectFeeSetting x, ProjectFeeSetting y)
        {
            int result = DateTime.Compare(x.StartDate.Date, y.StartDate.Date);
            if (result == 0)
            {
                result = x.ProjectFeeSettingId - y.ProjectFeeSettingId;
                result = result < -1 ? -1 : result > 1 ? 1 : result;
            }
            return result;
        }
    }

    public static class FinanceExtensions
    {

        /// <summary>
        /// Returns project fee for today
        /// </summary>
        public static int ProjectFee([NotNull] this Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return project.ProjectFee(DateTime.UtcNow);
        }

        /// <summary>
        /// Returns project fee for a specified date
        /// </summary>
        private static int ProjectFee(this Project project, DateTime operationDate)
            => project.ProjectFeeInfo(operationDate)?.Fee ?? 0;

        /// <summary>
        /// Returns fee info object for a specified date
        /// </summary>
        private static ProjectFeeSetting ProjectFeeInfo(this Project project, DateTime operationDate)
            => project.ProjectFeeSettings.Where(pfs => pfs.StartDate.Date <= operationDate.Date)
                .OrderByDescending(pfs => pfs, new ProjectFeeSettingComparer()).FirstOrDefault();

        /// <summary>
        /// Returns fee info object for today
        /// </summary>
        public static ProjectFeeSetting ProjectFeeInfo(this Project project)
            => project.ProjectFeeInfo(DateTime.UtcNow);

        /// <summary>
        /// Returns total sum of claim fee and all finance operations
        /// </summary>
        private static int ClaimTotalFee(this Claim claim, DateTime operationDate, int? fieldsFee)
            => claim.ClaimCurrentFee(operationDate, fieldsFee)
                + claim.ApprovedFinanceOperations.Sum(fo => fo.FeeChange);

        /// <summary>
        /// Returns total sum of claim fee and all finance operations using current date
        /// </summary>
        public static int ClaimTotalFee(this Claim claim, int? fieldsFee = null)
            => claim.ClaimTotalFee(DateTime.UtcNow, fieldsFee);

        /// <summary>
        /// Returns base fee (taken from project settings or claim's property CurrentFee)
        /// </summary>
        public static int BaseFee(this Claim claim, DateTime? operationDate = null)
            => claim.CurrentFee ?? claim.Project.ProjectFee(operationDate ?? DateTime.UtcNow);

        /// <summary>
        /// Returns actual fee for a claim (as a sum of claim fee and fields fee) using current date
        /// </summary>
        public static int ClaimCurrentFee(this Claim claim, int? fieldsFee)
            => claim.ClaimCurrentFee(DateTime.UtcNow, fieldsFee);

        /// <summary>
        /// Returns actual fee for a claim (as a sum of claim fee and fields fee)
        /// </summary>
        private static int ClaimCurrentFee(this Claim claim, DateTime operationDate, int? fieldsFee)
        { 
            return claim.BaseFee(operationDate)
                + claim.ClaimFieldsFee(fieldsFee);
            /******************************************************************
             * If you want to add additional fee to a claim's fee,
             * append your value to the expression above.
             * Example:
             *     return (claim.CurrentFee ?? claim.Project.CurrentFee(operationDate))
             *         + claim.ClaimFieldsFee(fieldsFee)
             *         + claim.AccommodationFee();
             *****************************************************************/
        }

        /// <summary>
        /// Returns claim payment status from total fee and money balance
        /// </summary>
        public static ClaimPaymentStatus GetClaimPaymentStatus(int totalFee, int balance)
        {
            if (totalFee < balance)
                return ClaimPaymentStatus.Overpaid;
            else if (totalFee == balance)
                return ClaimPaymentStatus.Paid;
            else if (balance > 0)
                return ClaimPaymentStatus.MoreToPay;
            else
                return ClaimPaymentStatus.NotPaid;
        }

        /// <summary>
        /// Returns claim payment status from claim' data
        /// </summary>
        public static ClaimPaymentStatus PaymentStatus(this Claim claim)
            => GetClaimPaymentStatus(claim.ClaimTotalFee(), claim.ClaimBalance());

        /// <summary>
        /// Returns current fee of a field with value
        /// </summary>
        public static int GetCurrentFee(this FieldWithValue self)
        {
            switch (self.Field.FieldType)
            {
                case ProjectFieldType.Checkbox:
                    return self.HasEditableValue ? self.Field.Price : 0;

                case ProjectFieldType.Number:
                    return self.ToInt() * self.Field.Price;

                case ProjectFieldType.Dropdown:
                case ProjectFieldType.MultiSelect:
                    return self.GetDropdownValues().Sum(v => v.Price);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Calculates total fields fee
        /// </summary>
        private static int CalcClaimFieldsFee(this Claim claim)
        {
            var values = claim.Project.GetFieldsNotFilled()
                .ToList()
                .FillIfEnabled(claim, claim.IsApproved ? claim.Character : null);

            return values.Sum(f => f.GetCurrentFee());
        }

        /// <summary>
        /// Returns actual total claim fields fee
        /// </summary>
        private static int ClaimFieldsFee(this Claim claim, int? fieldsFee)
        {
            if (fieldsFee == null)
                fieldsFee = claim.FieldsFee ?? claim.CalcClaimFieldsFee();
            // cache
            claim.FieldsFee = fieldsFee;

            return fieldsFee ?? 0;
        }

        /// <summary>
        /// Returns how many money left to pay
        /// </summary>
        public static int ClaimFeeDue(this Claim claim)
            => claim.ClaimTotalFee() - claim.ClaimBalance();

        /// <summary>
        /// Returns sum of all approved finance operations
        /// </summary>
        public static int ClaimBalance(this Claim claim)
            => claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);

        /// <summary>
        /// Returns sum of all unapproved finance operations
        /// </summary>
        public static int ClaimProposedBalance(this Claim claim)
            => claim.FinanceOperations.Sum(fo => fo.State == FinanceOperationState.Proposed ? fo.MoneyAmount : 0);

        public static void RequestModerationAccess(this FinanceOperation finance, int currentUserId)
    {
      if (!finance.Claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) &&
          finance.PaymentType?.UserId != currentUserId)
      {
        throw new NoAccessToProjectException(finance, currentUserId);
      }
    }

        public static bool ClaimPaidInFull(this Claim claim)
            => claim.ClaimBalance() >= claim.ClaimTotalFee();

        private static bool ClaimPaidInFull(this Claim claim, DateTime operationDate)
            => claim.ClaimBalance() >= claim.ClaimTotalFee(operationDate.AddDays(-1), null);

    public static void UpdateClaimFeeIfRequired(this Claim claim, DateTime operationDate)
    {
      if (claim.Project.ProjectFeeSettings.Any() //If project has fee 
          && claim.CurrentFee == null //and fee not already fixed for claim
          && claim.ClaimPaidInFull(operationDate) //and current fee is payed in full
        )
      {
        claim.CurrentFee = claim.Project.ProjectFee(operationDate); //fix fee for claim
      }
    }

    [CanBeNull]
    public static PaymentType GetCashPaymentType([NotNull] this ProjectAcl acl)
    {
      if (acl == null) throw new ArgumentNullException(nameof(acl));
      return acl.Project.PaymentTypes.SingleOrDefault(pt => pt.UserId == acl.UserId && pt.IsCash);
    }

    public static bool CanAcceptCash([NotNull] this Project project, [NotNull] User user)
    {
      if (project == null) throw new ArgumentNullException(nameof(project));
      if (user == null) throw new ArgumentNullException(nameof(user));
      return project.ProjectAcls.Single(acl => acl.UserId == user.UserId).CanAcceptCash();
    }

    public static bool CanAcceptCash([NotNull] this ProjectAcl projectAcl)
    {
      return projectAcl.GetCashPaymentType()?.IsActive ?? false;
    }

    }
}
