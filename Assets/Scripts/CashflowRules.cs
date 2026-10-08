// Pure rules shared by single-player and the local multiplayer adapter.
public static class CashflowRules
{
    public struct Balance { public int cash,loan; }
    public static int RequiredLoan(int cash,int cost)
    {
        if(cost<0)throw new System.ArgumentException("Negative cost");
        long shortfall=System.Math.Max(0L,(long)cost-cash);
        return checked((int)(((shortfall+999)/1000)*1000));
    }
    public static Balance Borrow(int cash,int loan,int amount)
    {
        if(loan<0||amount<=0||amount%1000!=0)throw new System.ArgumentException("Loans must be positive multiples of 1000");
        return new Balance{cash=checked(cash+amount),loan=checked(loan+amount)};
    }
    public static Balance Purchase(int cash,int loan,int cost,int additionalLoan)
    {
        if(loan<0||cost<0||additionalLoan<0||additionalLoan%1000!=0)throw new System.ArgumentException("Invalid purchase terms");
        int funded=checked(cash+additionalLoan);
        if(funded<cost)throw new System.ArgumentException("Insufficient cash");
        return new Balance{cash=checked(funded-cost),loan=checked(loan+additionalLoan)};
    }
    public static int NetCashflow(int income,int expenses)=>checked(income-expenses);
    public static bool IsBankrupt(int cash,int netCashflow)=>cash<0&&netCashflow<0;
    public static int SettlePayday(int cash,int income,int expenses)=>checked(cash+NetCashflow(income,expenses));
}
