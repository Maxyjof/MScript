# M 脚本语言
一个用于学习用途的垃圾脚本语言  Fucking trash language. Dont use it!!!<br />
<br />
## 语言介绍<br />
这是一个直接遍历AST并递归解释运行的解释型脚本语言<br />
温馨提示：语法设计上抄了python，C/C++，类的实例化过程抄了一点ts。<br />
坏消息：缝合怪<br />
好消息：全给我缝完了<br />
目前的功能制作清单（不一定会全部实现，因为只是做着玩的）：<br />
1.声明语句<br />
2.赋值语句<br />
3.if while for控制流语句<br />
4.函数定义<br />
5.类定义<br />
6.闭包<br />
7.内置全局变量<br />
8.内置全局函数<br />
9.new实例化<br />
10.load载入其他脚本文件<br />
==以下为未完成的==<br />
7.带参类构造函数<br />
8.类的继承<br />
9.三目运算符<br />
<br />
##语法介绍
### 1.声明语句<br />
变量名 = 值;<br />
示例：<br />
```
a = 3;
```
### 2.赋值语句<br />
略
### 3.if while for语句<br />
形同C/C++
### 4.函数定义<br />
def 函数名(参数序列){函数体}<br />
示例：
```
load System;
def Function()
{
  Write("Hello, world!");
}
```
### 5.类定义<br />
def 类名{类体}<br />
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
### 6.闭包<br />
闭包暂时不支持直接构造匿名函数的形式，但支持函数定义语法<br />
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
### 7.load<br />
load 相对路径或绝对路径;<br />
示例：
```
load System;
```
### 8.内置全局变量与内置全局函数<br />
目前只内置了一个全局变量，即__name__<br />
它的值只可能是main或module<br />
当当前代码所处的文件是被load载入的，值便是module，反之则是main<br />
内置全局函数必须通过call语句进行调用<br />
示例：
```
call Write("Hello, world!");
```
### 9.new实例化<br />
实例名 = new 类名();<br />
虽然目前没有做好类的有参构造函数的功能，但调用仍需写上一对小括号
### 10.数据类型转换<br />
未制作完毕，目前仅支持基本类型转换，当类类型转换时，会直接触发异常并报错<br />

## 内置全局函数一览<br />
def Write(text)<br />
无返回值；控制台中打印text文本并换行<br /><br />
def Read(text)<br />
返回读取到的字符串数据；控制台中打印text<br /><br />
def Pause()<br />
无返回值；暂停程序的运行，直到用户按下任意键<br /><br />
def QueryPerformanceCounter()<br />
返回整型数据；查询性能计数器中的计数，并返回<br /><br />
def QueryPerformanceFrequency()<br />
返回整形数据；查询性能频率，并返回<br /><br />
def TickCount()<br />
返回整形数据；获取系统环境的Tick计数，并返回<br /><br />

## 使用介绍
### 编写自己的代码<br />
新建一个文件夹作为项目本体，随后在里面新建一个文本文档，名称随意，后缀改为.ms，之后便可以开始编写M语言的代码了！<br />
### 运行自己写的代码<br />
首先编译好本项目后，在exe所处的目录中新建一个config.cfg文件。<br />
文件中写上
```
start 相对路径或绝对路径
```
这样，exe在启动的第一时刻，便会从cfg中寻找需要执行的主文件所处的路径位置，然后运行。<br />
