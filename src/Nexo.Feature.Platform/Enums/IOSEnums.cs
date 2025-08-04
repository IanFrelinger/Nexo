namespace Nexo.Feature.Platform.Enums
{
    /// <summary>
    /// Types of Swift source code files.
    /// </summary>
    public enum SwiftFileType
    {
        Model,
        ViewModel,
        Service,
        Repository,
        Manager,
        Utility,
        Extension,
        Protocol,
        Enum,
        Struct,
        Class
    }

    /// <summary>
    /// Types of SwiftUI view files.
    /// </summary>
    public enum SwiftUIViewType
    {
        ContentView,
        ListView,
        DetailView,
        FormView,
        NavigationView,
        TabView,
        ModalView,
        SheetView,
        AlertView,
        CustomView
    }

    /// <summary>
    /// Types of Core Data files.
    /// </summary>
    public enum CoreDataFileType
    {
        Model,
        PersistentContainer,
        ManagedObjectContext,
        Entity,
        Relationship,
        Migration
    }

    /// <summary>
    /// Types of Metal graphics files.
    /// </summary>
    public enum MetalFileType
    {
        VertexShader,
        FragmentShader,
        ComputeShader,
        RayTracingShader,
        PipelineState,
        RenderPipeline
    }

    /// <summary>
    /// Types of Core Data attributes.
    /// </summary>
    public enum CoreDataAttributeType
    {
        String,
        Integer16,
        Integer32,
        Integer64,
        Float,
        Double,
        Boolean,
        Date,
        Binary,
        Transformable,
        URI,
        UUID
    }

    /// <summary>
    /// Types of Core Data relationships.
    /// </summary>
    public enum CoreDataRelationshipType
    {
        ToOne,
        ToMany
    }

    /// <summary>
    /// Types of iOS UI patterns.
    /// </summary>
    public enum IOSUIPatternType
    {
        Navigation,
        TabBar,
        Modal,
        List,
        Form,
        Detail,
        MasterDetail,
        SplitView,
        Collection,
        Table,
        Card,
        Sheet,
        Alert,
        ActionSheet,
        Popover
    }

    /// <summary>
    /// Types of iOS performance optimizations.
    /// </summary>
    public enum IOSPerformanceType
    {
        Memory,
        Battery,
        Network,
        UI,
        Graphics,
        Storage,
        Caching,
        BackgroundProcessing
    }

    /// <summary>
    /// Types of Metal graphics features.
    /// </summary>
    public enum MetalGraphicsFeatureType
    {
        VertexProcessing,
        FragmentProcessing,
        ComputeProcessing,
        RayTracing,
        Tessellation,
        GeometryProcessing,
        TextureSampling,
        Blending,
        DepthTesting,
        StencilTesting
    }

    // Android-specific enums
    /// <summary>
    /// Types of Kotlin source code files.
    /// </summary>
    public enum KotlinFileType
    {
        Activity,
        Fragment,
        ViewModel,
        Repository,
        Service,
        Utility,
        Extension,
        Interface,
        DataClass,
        Object,
        Class
    }

    /// <summary>
    /// Types of Jetpack Compose view files.
    /// </summary>
    public enum ComposeViewType
    {
        Screen,
        Component,
        Dialog,
        BottomSheet,
        Navigation,
        List,
        Form,
        Custom
    }

    /// <summary>
    /// Types of Room database files.
    /// </summary>
    public enum RoomFileType
    {
        Database,
        Entity,
        Dao,
        Repository,
        Migration,
        TypeConverter
    }

    /// <summary>
    /// Types of Kotlin coroutines files.
    /// </summary>
    public enum CoroutinesFileType
    {
        CoroutineScope,
        Flow,
        Channel,
        Supervisor,
        Dispatcher,
        ExceptionHandler
    }

    /// <summary>
    /// Types of Room database columns.
    /// </summary>
    public enum RoomColumnType
    {
        String,
        Int,
        Long,
        Float,
        Double,
        Boolean,
        ByteArray,
        Date,
        Uri
    }

    /// <summary>
    /// Types of Room database relationships.
    /// </summary>
    public enum RoomRelationshipType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }

    /// <summary>
    /// Types of Android UI patterns.
    /// </summary>
    public enum AndroidUIPatternType
    {
        Navigation,
        BottomNavigation,
        TabLayout,
        MaterialDesign,
        List,
        Grid,
        Card,
        Dialog,
        BottomSheet
    }

    /// <summary>
    /// Types of Android performance optimizations.
    /// </summary>
    public enum AndroidPerformanceType
    {
        Memory,
        Battery,
        Network,
        UI,
        Database,
        ImageLoading
    }

    /// <summary>
    /// Types of Kotlin coroutines features.
    /// </summary>
    public enum KotlinCoroutinesFeatureType
    {
        Launch,
        Async,
        Flow,
        Channel,
        SupervisorScope,
        ExceptionHandler,
        Dispatcher
    }

    // Web-specific enums
    /// <summary>
    /// Types of JavaScript source code files.
    /// </summary>
    public enum JavaScriptFileType
    {
        Component,
        Hook,
        Service,
        Utility,
        Configuration,
        Test,
        Module,
        Bundle
    }

    /// <summary>
    /// Types of TypeScript source code files.
    /// </summary>
    public enum TypeScriptFileType
    {
        Component,
        Interface,
        Type,
        Service,
        Utility,
        Configuration,
        Test,
        Module,
        Bundle
    }

    /// <summary>
    /// Types of CSS files.
    /// </summary>
    public enum CSSFileType
    {
        Stylesheet,
        Module,
        Component,
        Theme,
        Variables,
        Animation,
        Responsive,
        Print
    }

    /// <summary>
    /// Types of HTML files.
    /// </summary>
    public enum HTMLFileType
    {
        Index,
        Template,
        Component,
        Layout,
        Page,
        Fragment,
        Email
    }

    /// <summary>
    /// Types of WebAssembly files.
    /// </summary>
    public enum WebAssemblyFileType
    {
        Module,
        Function,
        Memory,
        Table,
        Global,
        Import,
        Export,
        Custom
    }

    /// <summary>
    /// Types of web UI patterns.
    /// </summary>
    public enum WebUIPatternType
    {
        Component,
        Container,
        Presentational,
        HigherOrder,
        RenderProps,
        CustomHook,
        Context,
        Provider,
        Router,
        Layout
    }

    /// <summary>
    /// Types of web performance optimizations.
    /// </summary>
    public enum WebPerformanceType
    {
        CodeSplitting,
        LazyLoading,
        Caching,
        Minification,
        Compression,
        ImageOptimization,
        BundleOptimization,
        TreeShaking
    }

    /// <summary>
    /// Types of WebAssembly features.
    /// </summary>
    public enum WebAssemblyFeatureType
    {
        MemoryManagement,
        Threading,
        SIMD,
        ExceptionHandling,
        GarbageCollection,
        Debugging,
        Optimization,
        Interop
    }
} 