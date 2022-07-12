
# ファクトリーの委譲

## 概要

ファクトリークラスは、別のファクトリークラスの持つリゾルバーを頼りに依存関係を解決することができます。これを**ファクトリーの委譲**といいます。

あるファクトリークラス`A`が別のファクトリークラス`B`をプロパティとして持つとき、`A`がインスタンスを生成する処理をする際に依存先のインスタンスを生成する際に、`B`の提供するリゾルバーメソッドを使って生成することを候補に入れます。

この機能は、生成したいオブジェクトからの依存関係の中に、アプリ実行中のある時点まで待たないと解決できないものがあるシナリオを想定して作られました。

## やり方

まず、以下のようなクラス群があったとします。見ての通り、`Client`クラスが`Service`クラスに依存しています。

```csharp
namespace Sample;

class Client
{
    public Client(Service service)
    {
    }
}

class Service
{
}
```

これに対して、以下のような2つのファクトリーを書くことで**ファクトリーの委譲**を利用することができます。`ClientFactory`クラスが`ServiceFactory`クラスをプロパティとして持っていることに注目してください。 

```csharp
namespace Sample;

[Factory]
partial class ClientFactory
{
    public ServiceFactory BaseFactory { get; set; }
    public partial Client GetClient();
}

[Factory]
partial class ServiceFactory
{
    public partial Service GetService();
}
```

さて、`ClientFactory`クラスは`Service`クラスの生成を行わないようです。このままでは`Client`クラスを簡単には生成できず、`Service`クラスのインスタンスをコンストラクタの引数で要求するのではないでしょうか？

答えはNOで、今回の例ではImfactの持つ**ファクトリーの委譲**機能によって、`ClientFactory.GetClient`リゾルバーメソッドは以下のように生成されます。

```csharp
public partial Client GetClient()
{
    using var scope = __resolverService.Enter();
    Client? result;
    
    result = new Client(BaseFactory.GetService());
    
    return result;
}
```

このリゾルバーは、`Service`クラスを生成するために`ServiceFactory.GetService`メソッドに依存関係の解決を任せています。これによって`ClientFactory`クラスは、`Service`クラスのインスタンスをコンストラクタの引数で要求することなく`Client`クラスのインスタンスを生成できます。

### 委譲されるファクトリーはどこから来る？

コンストラクタは以下のようになります。`BaseFactory`プロパティに代入する実際のインスタンスは、コンストラクタの引数を通じて要求されていることが分かります。

```csharp
internal ClientFactory(ServiceFactory baseFactory)
{
    BaseFactory = baseFactory;
    
    __resolverService = new ResolverService();
    this.RegisterService(__resolverService);
}
```

### 生成コード全体

実際に`ClientFactory`クラスに対して生成されるコード全体は以下のようになります。

```csharp
// <autogenerated />
#nullable enable
using Imfact.Annotations;
using Sample;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sample
{
	partial class ClientFactory
	{
		private protected ResolverService __resolverService;
		
		internal ClientFactory(ServiceFactory baseFactory)
		{
			BaseFactory = baseFactory;
			
			__resolverService = new ResolverService();
			this.RegisterService(__resolverService);
		}
		
		internal void RegisterService(ResolverService service)
		{
			__resolverService = service;
			BaseFactory.RegisterService(service);
		}
		
		public partial Client GetClient()
		{
			using var scope = __resolverService.Enter();
			Client? result;
			
			result = new Client(BaseFactory.GetService());
			
			return result;
		}
		
		public void Export(Imfact.Annotations.IServiceImporter importer)
		{
			importer.Import<Client>(() => GetClient());
		}
	}
}
```

## 便利な使い道：クラスを段階的に構築する

ファクトリーの委譲を使えば、アプリ実行中のある時点で初めて生成できるようなインスタンスに関する依存関係も解決することができます。

例として、以下のような2つのクラスがあったとします。`GoldPhase`クラスは`SilverPhase`クラスに依存するだけでなく、`IdContext`というクラスのインスタンスを引数として必要としています。

```csharp
class IdContext
{
    public IdContext(int id)
    {
    }
}

class SilverPhase
{
}

class GoldPhase
{
    public GoldPhase(SilverPhase silver, IdContext context)
    {
    }
}
```

ここで、もし`IdContext`クラスのインスタンスが、アプリ実行中のある時点までは生成できなくて、しかも`SilverPhase`クラスのインスタンスはその時点より前でも利用したい場合を考えてみましょう。

たとえば、アプリユーザーの入力があって初めてどのような内容の`IdContext`を生成すればいいか判明するような場面です。

### 単純なファクトリー定義にしてみる

このとき、以下のようなファクトリー定義では要求を満たせません。

```csharp
partial class MyFactory
{
    [Cache]
    public partial SilverPhase GetSilver();
    [Cache]
    public partial GoldPhase GetGold();
}
```

このファクトリー定義では、`GoldPhase`クラスのインスタンスを生成するためには`IdContext`クラスが必要なため、最終的に`MyFactory`クラスのコンストラクタが`IdContext`クラスのインスタンスを引数として要求します。

ところが、そのコンストラクタ引数はアプリ実行中のある時点ではじめて生成できるようになるもので、しかも`GetSilver`メソッドによる解決を試みたいのはその時点より前です。結果として、プログラマーは`MyFactory`ファクトリークラスのインスタンス化を`SilverPhase`クラスのインスタンスが欲しい時点に間に合わせることができません。

### IdContextクラスも解決させてみる

かといって、以下のように書いてみてもうまくいきません。

```csharp
partial class MyFactory
{
    [Cache]
    public partial IdContext GetIdContext(int id);
    [Cache]
    public partial SilverPhase GetSilver();
    [Cache]
    public partial GoldPhase GetGold();
}
```

この場合、`IdContext`クラスのインスタンスを生成するために`IdContext`クラスのコンストラクタ引数を指定しなければなりません。そのため、`GetGold`メソッドの実装内で`IdContext`を生成できず、代わりにファクトリーのコンストラクタ引数を通じてint型の値を要求され、前節と似た状態に陥ります。

### 解決案

今回の例では、以下のような2つのファクトリークラスを作るのがシンプルです。

```csharp
partial class SilverFactory
{
    [Cache]
    public partial SilverPhase GetSilver();
}

partial class GoldFactory
{
    private SilverFactory BaseFactory { get; }
    [Cache]
    public partial GoldPhase GetGold();
}
```

`GetGold`リゾルバーは以下のようになります。`_idContext`

```csharp
public partial GoldPhase GetGold()
{
    using var scope = __resolverService.Enter();
    GoldPhase? result;
    
    result = _GetGold_Cache.Before();
    if(result is null)
    {
        result = new GoldPhase(BaseFactory.GetSilver(), _idContext);
    }
    result = _GetGold_Cache.After(result);
    
    return result;
}
```

重要なのは以下の部分です。

```csharp
result = new GoldPhase(BaseFactory.GetSilver(), _idContext);
```

`GoldFactory`は、`SilverPhase`の生成を委譲されたファクトリーである`SilverFactory`に依頼します。しかも、`GoldFactory`ファクトリークラスを生成する時点では`IdContext`クラスのインスタンスが用意できている前提ですので、`GoldFactory`クラスがコンストラクタ引数として`IdContext`クラスを要求していても問題ありません。

そして重要なのは、この書き方ならば`SilverFactory`ファクトリークラスはコンストラクタ引数を通じて`IdContext`クラスを要求しないことです。これなら、生成のタイミングに間に合うかどうかを気にせずに`SilverPhase`の依存関係を解決することができます。

そして、`GoldFactory`のコンストラクタは以下のようになります。

```csharp
internal GoldFactory(IdContext idContext, SilverFactory baseFactory)
{
    _idContext = idContext;
    BaseFactory = baseFactory;
    
    _GetGold_Cache = new Cache<GoldPhase>();
    
    __resolverService = new ResolverService();
    this.RegisterService(__resolverService);
}
```

このファクトリーの生成のために`IdContext`型の引数を要求していますね。しかしここまで述べた通り、`GoldPhase`のインスタンスを生成できるようにするのは`IdContext`を与えられるようになってからで構わないわけです。

### まとめ

ファクトリーの委譲を利用して、`IdContext`クラスのインスタンスが生成できる時点より前か後かの区別にしたがって、ファクトリークラスを分割することができました。

この機能は例えば、何らかのエディターを開発している際に、編集するファイルを開くまでファイルに関する情報を用意できない場合に有用です。