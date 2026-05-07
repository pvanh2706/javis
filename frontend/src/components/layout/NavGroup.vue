<template>
  <div class="mb-1">
    <p class="px-4 py-1 text-xs font-semibold text-stone-400 uppercase tracking-wider">{{ label }}</p>
    <slot />
  </div>
</template>
<script setup lang="ts">
defineProps<{ label: string }>()
</script>
