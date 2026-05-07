<template>
  <RouterLink
    :to="to"
    class="flex items-center gap-2.5 px-4 py-2 text-sm text-stone-300 hover:bg-stone-800 hover:text-white transition-colors"
    activeClass="bg-stone-800 text-white border-l-2 border-sienna"
  >
    <component :is="iconComponent" :size="16" />
    <slot />
  </RouterLink>
</template>
<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import * as icons from 'lucide-vue-next'

const props = defineProps<{ to: string; icon: string }>()
const iconComponent = computed(() => (icons as Record<string, unknown>)[props.icon])
</script>
