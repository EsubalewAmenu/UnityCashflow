// Pure rules shared by single-player and the local multiplayer adapter.
public static class CashflowRules
{
    public static int NetCashflow(int income,int expenses)=>checked(income-expenses);
    public static bool IsBankrupt(int cash,int netCashflow)=>cash<0&&netCashflow<0;
    public static int SettlePayday(int cash,int income,int expenses)=>checked(cash+NetCashflow(income,expenses));
}
