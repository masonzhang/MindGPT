import { IApi } from 'umi';

export default (api: IApi) => {
    api.modifyHTML(($) => {
        return $;
    });
    api.addHTMLMetas(() => [{ name: 'foo', content: 'bar' }]);
    api.addHTMLLinks(() => [{ rel: 'foo', content: 'bar' }]);
    api.addHTMLStyles(() => [`body { color: red; }`]);
    api.addHTMLHeadScripts(() => [`
    !(function (n, e) {
        function setViewHeight() {
          const windowVH = e.innerHeight / 100;
          n.documentElement.style.setProperty('--vh', windowVH + 'px')
        }

        const i = 'orientationchange' in window ? 'orientationchange' : 'resize';
        n.addEventListener('DOMContentLoaded', setViewHeight)
        e.addEventListener(i, setViewHeight)
      })(document, window)

      window.addEventListener('resize', function() {
        if (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA') {
          window.setTimeout(function() {
            if ('scrollIntoView' in document.activeElement) {
              document.activeElement.scrollIntoView();
            } else {
              document.activeElement.scrollIntoViewIfNeeded();
            }
          }, 0);
        }
      });
    `]);
    api.addHTMLScripts(() => [`console.log('hello world')`]);
    api.addEntryCodeAhead(() => [`console.log('entry code ahead')`]);
    api.addEntryCode(() => [`console.log('entry code')`]);
    api.onDevCompileDone((opts) => {
        opts;
        // console.log('> onDevCompileDone', opts.isFirstCompile);
    });
    api.onBuildComplete((opts) => {
        opts;
        // console.log('> onBuildComplete', opts.isFirstCompile);
    });
    api.chainWebpack((memo) => {
        memo;
    });
    api.onStart(() => {});
    api.onCheckCode((args) => {
        args;
        // console.log('> onCheckCode', args);
    });
};
