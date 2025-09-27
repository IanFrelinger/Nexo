namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
        private string GetNextJsPageTemplate()
        {
            return @"import { NextPage } from 'next';
import Head from 'next/head';

interface {{ComponentName}}Props {
  // Add your props here
}

const {{ComponentName}}: NextPage<{{ComponentName}}Props> = ({ }) => {
  return (
    <>
      <Head>
        <title>{{ComponentName}}</title>
        <meta name=\"description\" content=\"Generated with {{Framework}}\" />
      </Head>
      <div className=\"{{ComponentName}}-container\"> 
        <h1>{{ComponentName}}</h1>
        <p>Generated with {{Framework}}</p>
        {{SourceCode}}
      </div>
    </>
  );
};

export default {{ComponentName}};";
        }

        private string GetNextJsFunctionalTemplate()
        {
            return @"import React from 'react';

interface {{ComponentName}}Props {
  // Add your props here
}

export default function {{ComponentName}}({ }: {{ComponentName}}Props) {
  return (
    <div className=\"{{ComponentName}}-container\">
      <h1>{{ComponentName}}</h1>
      <p>Generated with {{Framework}}</p>
      {{SourceCode}}
    </div>
  );
}";
        }

        private string GetNuxtJsPageTemplate()
        {
            return @"<template>
  <div class=\"{{ComponentName}}-container\">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=\"ts\">
definePageMeta({
  title: '{{ComponentName}}',
  description: 'Generated with {{Framework}}'
});

// Page logic here
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }

        private string GetNuxtJsFunctionalTemplate()
        {
            return @"<template>
  <div class=\"{{ComponentName}}-container\">
    <h1>{{ComponentName}}</h1>
    <p>Generated with {{Framework}}</p>
    {{SourceCode}}
  </div>
</template>

<script setup lang=\"ts\">
// Component logic here
</script>

<style scoped>
.{{ComponentName}}-container {
  /* Add your styles here */
}
</style>";
        }
    }
}

