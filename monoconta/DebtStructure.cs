namespace monoconta
{
    public class DebtStructure
    {
        public DebtStructure(Entity lender, Entity borrower, double rate, double sum)
        {
            this.Creditor = lender;
            this.Debtor = borrower;
            this.InterestRate = rate;
            this.Sum = sum;
        }

        public Entity Creditor { get; set; }
        public Entity Debtor { get; set; }
        public double InterestRate { get; set; }
        public double Sum { get; set; }
    }
}