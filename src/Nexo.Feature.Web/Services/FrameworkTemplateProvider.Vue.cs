namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
        private string GetVueCompositionTemplate()
        {
            return @"<template>
  <div class="{{ComponentName}}-container">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';

// Props
interface Props {
  // Add your props here
}

const props = defineProps<Props>();

// Reactive state
const state = ref<string>('');

// Computed properties
const computedValue = computed(() => {
  return state.value.toUpperCase();
});

// Methods
const handleClick = () => {
  // Handle click events
};

// Lifecycle
onMounted(() => {
  // Component initialization logic
});
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetVueOptionsTemplate()
        {
            return @"<template>
  <div class="{{ComponentName}}-container">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

export default defineComponent({
  name: '{{ComponentName}}',
  props: {
    // Add your props here
  },
  data() {
    return {
      state: ''
    };
  },
  computed: {
    computedValue(): string {
      return this.state.toUpperCase();
    }
  },
  methods: {
    handleClick() {
      // Handle click events
    }
  },
  mounted() {
    // Component initialization logic
  }
});
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetVuePureTemplate()
        {
            return @"<template>
  <div class="{{ComponentName}}-container">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}} (Pure Component)</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';

// Props only - no internal state
interface Props {
  // Add your props here
}

defineProps<Props>();
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetVueComposableTemplate()
        {
            return @"import { ref, computed } from 'vue';

export function use{{ComponentName}}() {
  const state = ref<string>('');

  const computedValue = computed(() => {
    return state.value.toUpperCase();
  });

  const updateState = (newState: string) => {
    state.value = newState;
  };

  return {
    state,
    computedValue,
    updateState
  };
}";
        }
    }
}

