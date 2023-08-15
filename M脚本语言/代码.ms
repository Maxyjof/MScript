load System;

def Fib(n)
{
    if(n < 2)
        return n;
    first = 0;
    second = 1;
    sum = 0;
    for(i = 1;i < n;i = i + 1)
    {
        first = second;
        second = sum;
    }
    return sum;
}

num = 1000000;
timer = new Timer();
timer.Start();
num = Fib(num);
timer.Stop();
//Write(num);
Write("time£º");
Write(timer.GetIntervalTime());
