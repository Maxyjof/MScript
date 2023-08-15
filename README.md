# M 脚本语言
一个用于学习用途的垃圾脚本语言<br />
<br />
##语言介绍
这是一个直接遍历AST并递归解释运行的解释型脚本语言
目前的功能制作清单（不一定会全部实现，因为只是做着玩的）：
1.声明语句
2.赋值语句
3.if while for控制流语句
4.函数定义
5.类定义
6.闭包
7.内置全局变量
8.内置全局函数
9.new实例化
10.load载入其他脚本文件
（以下为未完成的）
7.带参类构造函数
8.类的继承
9.三目运算符

##语法介绍
###1.声明语句
变量名 = 值;
示例：
```
a = 3;
```
###2.赋值语句
略
###3.if while for语句
形同C/C++
###4.函数定义
def 函数名(参数序列){函数体}
示例：
```
load System;
def Function()
{
  Write("Hello, world!");
}
```
###5.类定义
def 类名{类体}
示例
```
def Entity
{
  Name;
  def SetName(n)
  {
    Name = n;
  }
  def GetName()
  {
    return Name;
  }
  Name = "";
}
//Tip：类中的语句都会在类被实例化时执行，充当了无参构造函数的作用
```
###6.闭包
闭包暂时不支持直接构造匿名函数的形式，但支持函数定义语法
示例：
```
def GetFunc()
{
  Num = 3;
  def func()
  {
    return Num;
  }
  Num = 4;
  return func;
}

Write(GetFunc()());
//输出结果：4
```
###7.load
load 相对路径或绝对路径;
示例：
```
load System;
```
###8.内置全局变量与内置全局函数
目前只内置了一个全局变量，即__name__
它的值只可能是main或module
当当前代码所处的文件是被load载入的，值便是module，反之则是main
内置全局函数必须通过call语句进行调用
示例：
```
call Print("Hello, world!");
```
###9.new实例化
实例名 = new 类名();
虽然目前没有做好类的有参构造函数的功能，但调用仍需写上一对小括号
